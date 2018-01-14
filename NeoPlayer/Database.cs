using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlServerCe;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NeoPlayer.Models;

namespace NeoPlayer
{
	static class Database
	{
		static readonly string DB_FILE = Path.Combine(Path.GetDirectoryName(typeof(App).Assembly.Location), "NeoPlayer.db");
		static public bool Exists => File.Exists(DB_FILE);

		static SqlCeConnection conn = null;

		static Database()
		{
			var cs = new SqlCeConnectionStringBuilder { DataSource = DB_FILE }.ToString();

			var create = !Exists;
			if (create)
				new SqlCeEngine(cs).CreateDatabase();

			conn = new SqlCeConnection(cs);
			conn.Open();

			if (create)
			{
				ExecuteNonQueryAsync("CREATE TABLE Version (CurrentVersion INT)").Wait();
				ExecuteNonQueryAsync("INSERT INTO Version (CurrentVersion) VALUES (0)").Wait();
			}

			UpdateToLatestAsync().Wait();

			if ((create) && (Helpers.Debug))
			{
				AddOrUpdateAsync(new Shortcut { Name = "cool", Value = "https://www.youtube.com/playlist?list=PLzDWcvdzYAvqF6Dk6bWXyKcMQRbUCQaKp" }).Wait();
				AddOrUpdateAsync(new Shortcut { Name = "test", Value = "https://www.youtube.com/playlist?list=PLzDWcvdzYAvr2C958wB8Rh3JMnTb_UkuX" }).Wait();
				AddOrUpdateAsync(new Shortcut { Name = "video", Value = "https://www.youtube.com/watch?v=jcBYfBGCKMk" }).Wait();
			}
		}

		async static Task UpdateToLatestAsync()
		{
			var version = ExecuteScalar<int>("SELECT TOP 1 CurrentVersion FROM Version");
			while (true)
			{
				++version;
				var method = typeof(Database).GetMethod($"UpdateTo{version}", BindingFlags.Static | BindingFlags.NonPublic);
				if (method == null)
					break;
				method.Invoke(null, new object[] { });
				await ExecuteNonQueryAsync("UPDATE Version SET CurrentVersion = @Version", new Dictionary<string, object> { ["@Version"] = version });
			}
		}

		async static void UpdateTo1()
		{
			await ExecuteNonQueryAsync(@"
CREATE TABLE VideoFile
(
	VideoFileID INT NOT NULL PRIMARY KEY IDENTITY(1, 1),
	Identifier NVARCHAR(256) NOT NULL,
	Title NVARCHAR(1024) NOT NULL,
	FileName NVARCHAR(1024) NOT NULL,
	CONSTRAINT uk_VideoFile_Identifier UNIQUE (Identifier)
)");
			await ExecuteNonQueryAsync(@"
CREATE TABLE Shortcut
(
	ShortcutID INT NOT NULL PRIMARY KEY IDENTITY(1, 1),
	Name NVARCHAR(50) NOT NULL,
	Value NVARCHAR(1024) NOT NULL,
	CONSTRAINT uk_Shortcut_Name UNIQUE (Name)
)");
		}

		static List<PropertyInfo> GetProperties<T>() => typeof(T).GetProperties().Where(prop => prop.GetCustomAttribute<IgnoreAttribute>() == null).ToList();

		static PropertyInfo GetPrimaryKey(List<PropertyInfo> properties)
		{
			var primaryKeyProp = properties.Where(prop => prop.GetCustomAttribute<PrimaryKeyAttribute>() != null).FirstOrDefault();
			if (primaryKeyProp == null)
				throw new Exception($"Unable to identify primary key");
			return primaryKeyProp;
		}

		static PropertyInfo GetPrimaryKeyProp<T>() => GetPrimaryKey(GetProperties<T>());

		async static Task ExecuteNonQueryAsync(string query, Dictionary<string, object> parameters = null)
		{
			using (var cmd = new SqlCeCommand(query, conn))
			{
				AddParameters(cmd, parameters);
				await cmd.ExecuteNonQueryAsync();
			}
		}

		static int GetInsertID() => ExecuteScalar<int>("SELECT @@IDENTITY");

		async static public Task AddOrUpdateAsync<T>(T obj)
		{
			var properties = GetProperties<T>();
			var primaryKeyProp = GetPrimaryKey(properties);
			properties.Remove(primaryKeyProp);
			var parameters = properties.ToDictionary(prop => $"@{prop.Name}", prop => prop.GetValue(obj));

			string query;
			var primaryKey = (int)primaryKeyProp.GetValue(obj);
			if (primaryKey == 0)
				query = $"INSERT INTO {typeof(T).Name} ({string.Join(", ", properties.Select(prop => prop.Name))}) VALUES ({string.Join(", ", properties.Select(prop => $"@{prop.Name}"))})";
			else
			{
				query = $"UPDATE {typeof(T).Name} SET {string.Join(", ", properties.Select(prop => $"{prop.Name} = @{prop.Name}"))} WHERE {primaryKeyProp.Name} = @ID";
				parameters["@ID"] = primaryKey;
			}

			await ExecuteNonQueryAsync(query, parameters);

			if (primaryKey == 0)
				primaryKeyProp.SetValue(obj, GetInsertID());
		}

		static async public Task DeleteAsync<T>(int id)
		{
			var primaryKeyProp = GetPrimaryKeyProp<T>();
			await ExecuteNonQueryAsync($"DELETE FROM {typeof(T).Name} WHERE {primaryKeyProp.Name} = @ID", new Dictionary<string, object> { ["@ID"] = id });
		}

		static async public Task DeleteAsync<T>(T obj)
		{
			var primaryKeyProp = GetPrimaryKeyProp<T>();
			await ExecuteNonQueryAsync($"DELETE FROM {typeof(T).Name} WHERE {primaryKeyProp.Name} = @ID", new Dictionary<string, object> { ["@ID"] = primaryKeyProp.GetValue(obj) });
		}

		static async public Task<List<T>> GetAsync<T>(string where = null, Dictionary<string, object> parameters = null)
		{
			var result = new List<T>();
			var props = GetProperties<T>();
			using (var reader = await ExecuteReaderAsync($"SELECT {string.Join(", ", props.Select(prop => prop.Name))} FROM {typeof(T).Name}{(string.IsNullOrWhiteSpace(where) ? "" : $" WHERE {where}")}", parameters))
			{
				var names = Enumerable.Range(0, reader.FieldCount).ToDictionary(index => reader.GetName(index), index => index, StringComparer.OrdinalIgnoreCase);
				while (await reader.ReadAsync())
				{
					var item = (T)Activator.CreateInstance(typeof(T));
					foreach (var prop in props)
					{
						var value = reader.GetValue(names[prop.Name]);
						if (value is DBNull)
							value = null;
						prop.SetValue(item, value);
					}

					result.Add(item);
				}
			}
			return result;
		}

		static async public Task<T> GetAsync<T>(int primaryKeyID) => (await GetAsync<T>($"{GetPrimaryKeyProp<T>().Name} = @ID", new Dictionary<string, object> { ["@ID"] = primaryKeyID })).FirstOrDefault();

		static T ExecuteScalar<T>(string query, Dictionary<string, object> parameters = null)
		{
			object result;
			using (var cmd = new SqlCeCommand(query, conn))
			{
				AddParameters(cmd, parameters);
				result = cmd.ExecuteScalar();
			}
			return (T)Convert.ChangeType(result, typeof(T));
		}

		static async Task<DbDataReader> ExecuteReaderAsync(string query, Dictionary<string, object> parameters = null)
		{
			using (var cmd = new SqlCeCommand(query, conn))
			{
				AddParameters(cmd, parameters);
				return await cmd.ExecuteReaderAsync();
			}
		}

		static void AddParameters(SqlCeCommand cmd, Dictionary<string, object> parameters)
		{
			if (parameters != null)
				foreach (var pair in parameters)
					cmd.Parameters.AddWithValue(pair.Key, pair.Value ?? DBNull.Value);
		}

		static public void Close()
		{
			conn?.Dispose();
			conn = null;
		}
	}
}

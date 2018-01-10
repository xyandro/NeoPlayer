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
	class Database : IDisposable
	{
		readonly string DB_FILE = Path.Combine(Path.GetDirectoryName(typeof(App).Assembly.Location), "NeoPlayer.db");
		public bool Exists => File.Exists(DB_FILE);

		SqlCeConnection conn = null;

		public Database()
		{
			var cs = new SqlCeConnectionStringBuilder { DataSource = DB_FILE }.ToString();

			var create = !Exists;
			if (create)
				new SqlCeEngine(cs).CreateDatabase();

			conn = new SqlCeConnection(cs);
			conn.Open();

			if (create)
			{
				ExecuteNonQuery("CREATE TABLE Version (CurrentVersion INT)");
				ExecuteNonQuery("INSERT INTO Version (CurrentVersion) VALUES (0)");
			}

			UpdateToLatest();
		}

		void UpdateToLatest()
		{
			var version = ExecuteScalar<int>("SELECT TOP 1 CurrentVersion FROM Version");
			while (true)
			{
				++version;
				var method = typeof(Database).GetMethod($"UpdateTo{version}", BindingFlags.Instance | BindingFlags.NonPublic);
				if (method == null)
					break;
				method.Invoke(this, new object[] { });
				ExecuteNonQuery("UPDATE Version SET CurrentVersion = @Version", new Dictionary<string, object> { ["@Version"] = version });
			}
		}

		void UpdateTo1()
		{
			ExecuteNonQuery("CREATE TABLE VideoFile (VideoFileID INT NOT NULL PRIMARY KEY IDENTITY(1, 1), Identifier NVARCHAR(256), FileName NVARCHAR(1024), Title NVARCHAR(1024))");
		}

		List<PropertyInfo> GetProperties<T>() => typeof(T).GetProperties().Where(prop => prop.GetCustomAttribute<IgnoreAttribute>() == null).ToList();

		void ExecuteNonQuery(string query, Dictionary<string, object> parameters = null)
		{
			using (var cmd = new SqlCeCommand(query, conn))
			{
				AddParameters(cmd, parameters);
				cmd.ExecuteNonQuery();
			}
		}

		int GetInsertID() => ExecuteScalar<int>("SELECT @@IDENTITY");

		public void AddOrUpdate<T>(T obj)
		{
			var properties = GetProperties<T>();
			var primaryKeyProp = properties.Where(prop => prop.GetCustomAttribute<PrimaryKeyAttribute>() != null).FirstOrDefault();
			if (primaryKeyProp == null)
				throw new Exception($"Unable to identify primary key for type {typeof(T).Name}");

			string query;
			var primaryKey = (int)primaryKeyProp.GetValue(obj);
			if (primaryKey == 0)
			{
				properties.Remove(primaryKeyProp);
				query = $"INSERT INTO {typeof(T).Name} ({string.Join(", ", properties.Select(prop => prop.Name))}) VALUES ({string.Join(", ", properties.Select(prop => $"@{prop.Name}"))})";
			}
			else
				query = $"UPDATE {typeof(T).Name} SET {string.Join(", ", properties.Select(prop => $"{prop.Name} = @{prop.Name}"))} WHERE {primaryKeyProp.Name} = @{primaryKeyProp.Name}";

			var parameters = properties.ToDictionary(prop => prop.Name, prop => prop.GetValue(obj));

			ExecuteNonQuery(query, parameters);

			if (primaryKey == 0)
				primaryKeyProp.SetValue(obj, GetInsertID());
		}

		async public Task<List<T>> GetAsync<T>(string where = null, Dictionary<string, object> parameters = null)
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

		T ExecuteScalar<T>(string query, Dictionary<string, object> parameters = null)
		{
			object result;
			using (var cmd = new SqlCeCommand(query, conn))
			{
				AddParameters(cmd, parameters);
				result = cmd.ExecuteScalar();
			}
			return (T)Convert.ChangeType(result, typeof(T));
		}

		async Task<DbDataReader> ExecuteReaderAsync(string query, Dictionary<string, object> parameters = null)
		{
			using (var cmd = new SqlCeCommand(query, conn))
			{
				AddParameters(cmd, parameters);
				return await cmd.ExecuteReaderAsync();
			}
		}

		void AddParameters(SqlCeCommand cmd, Dictionary<string, object> parameters)
		{
			if (parameters != null)
				foreach (var pair in parameters)
					cmd.Parameters.AddWithValue(pair.Key, pair.Value ?? DBNull.Value);
		}

		public void Dispose()
		{
			conn?.Dispose();
			conn = null;
		}
	}
}

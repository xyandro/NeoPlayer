using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace NeoPlayerTestClient
{
	partial class MainWindow
	{
		public static DependencyProperty SearchTextProperty = DependencyProperty.Register(nameof(SearchText), typeof(string), typeof(MainWindow));
		public static DependencyProperty CurrentSongProperty = DependencyProperty.Register(nameof(CurrentSong), typeof(string), typeof(MainWindow));
		public static DependencyProperty PositionProperty = DependencyProperty.Register(nameof(Position), typeof(int), typeof(MainWindow));
		public static DependencyProperty LengthProperty = DependencyProperty.Register(nameof(Length), typeof(int), typeof(MainWindow));
		public static DependencyProperty VideosProperty = DependencyProperty.Register(nameof(Videos), typeof(ObservableCollection<VideoData>), typeof(MainWindow));

		public string SearchText { get { return (string)GetValue(SearchTextProperty); } set { SetValue(SearchTextProperty, value); } }
		public string CurrentSong { get { return (string)GetValue(CurrentSongProperty); } set { SetValue(CurrentSongProperty, value); } }
		public int Position { get { return (int)GetValue(PositionProperty); } set { SetValue(PositionProperty, value); } }
		public int Length { get { return (int)GetValue(LengthProperty); } set { SetValue(LengthProperty, value); } }
		public ObservableCollection<VideoData> Videos { get { return (ObservableCollection<VideoData>)GetValue(VideosProperty); } set { SetValue(VideosProperty, value); } }

		public MainWindow()
		{
			InitializeComponent();
			SearchText = "My Search Text";
			CurrentSong = "Current song";
			Position = 125;
			Length = 255;
			Videos = new ObservableCollection<VideoData>
			{
				new VideoData { Name = "Randon Spackman",    Description = "Randon", IsQueued = true  },
				new VideoData { Name = "Ben Christensen",    Description = "Ben",    IsQueued = true  },
				new VideoData { Name = "Sophie Christensen", Description = "Sophie", IsQueued = true  },
				new VideoData { Name = "Timo Christensen",   Description = "Timo",   IsQueued = false },
				new VideoData { Name = "Kate Spackman",      Description = "Kate",   IsQueued = true  },
				new VideoData { Name = "Phoebe Christensen", Description = "Phoebe", IsQueued = true  },
				new VideoData { Name = "Megan Spackman",     Description = "Megan",  IsQueued = false },
				new VideoData { Name = "Randon Spackman",    Description = "Randon", IsQueued = true  },
				new VideoData { Name = "Ben Christensen",    Description = "Ben",    IsQueued = true  },
				new VideoData { Name = "Sophie Christensen", Description = "Sophie", IsQueued = true  },
				new VideoData { Name = "Timo Christensen",   Description = "Timo",   IsQueued = false },
				new VideoData { Name = "Kate Spackman",      Description = "Kate",   IsQueued = true  },
				new VideoData { Name = "Phoebe Christensen", Description = "Phoebe", IsQueued = true  },
				new VideoData { Name = "Megan Spackman",     Description = "Megan",  IsQueued = false },
				new VideoData { Name = "Randon Spackman",    Description = "Randon", IsQueued = true  },
				new VideoData { Name = "Ben Christensen",    Description = "Ben",    IsQueued = true  },
				new VideoData { Name = "Sophie Christensen", Description = "Sophie", IsQueued = true  },
				new VideoData { Name = "Timo Christensen",   Description = "Timo",   IsQueued = false },
				new VideoData { Name = "Kate Spackman",      Description = "Kate",   IsQueued = true  },
				new VideoData { Name = "Phoebe Christensen", Description = "Phoebe", IsQueued = true  },
				new VideoData { Name = "Megan Spackman",     Description = "Megan",  IsQueued = false },
				new VideoData { Name = "Randon Spackman",    Description = "Randon", IsQueued = true  },
				new VideoData { Name = "Ben Christensen",    Description = "Ben",    IsQueued = true  },
				new VideoData { Name = "Sophie Christensen", Description = "Sophie", IsQueued = true  },
				new VideoData { Name = "Timo Christensen",   Description = "Timo",   IsQueued = false },
				new VideoData { Name = "Kate Spackman",      Description = "Kate",   IsQueued = true  },
				new VideoData { Name = "Phoebe Christensen", Description = "Phoebe", IsQueued = true  },
				new VideoData { Name = "Megan Spackman",     Description = "Megan",  IsQueued = false },
				new VideoData { Name = "Randon Spackman",    Description = "Randon", IsQueued = true  },
				new VideoData { Name = "Ben Christensen",    Description = "Ben",    IsQueued = true  },
				new VideoData { Name = "Sophie Christensen", Description = "Sophie", IsQueued = true  },
				new VideoData { Name = "Timo Christensen",   Description = "Timo",   IsQueued = false },
				new VideoData { Name = "Kate Spackman",      Description = "Kate",   IsQueued = true  },
				new VideoData { Name = "Phoebe Christensen", Description = "Phoebe", IsQueued = true  },
				new VideoData { Name = "Megan Spackman",     Description = "Megan",  IsQueued = false },
			};

			SocketClient.RunSocket();
		}
	}

	public class SecondsToTimeConverter : MarkupExtension, IValueConverter
	{
		public override object ProvideValue(IServiceProvider serviceProvider) => this;

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if ((value == null) || (!(value is int)))
				return DependencyProperty.UnsetValue;
			return TimeSpan.FromSeconds((int)value).ToString(@"m\:ss");
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) { throw new NotImplementedException(); }
	}
}

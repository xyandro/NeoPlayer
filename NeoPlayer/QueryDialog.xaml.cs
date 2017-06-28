using System.Windows;

namespace NeoPlayer
{
	partial class QueryDialog
	{
		static DependencyProperty SlidesQueryProperty = DependencyProperty.Register(nameof(SlidesQuery), typeof(string), typeof(QueryDialog));
		static DependencyProperty SlidesSizeProperty = DependencyProperty.Register(nameof(SlidesSize), typeof(string), typeof(QueryDialog));
		static DependencyProperty SlideDisplayTimeProperty = DependencyProperty.Register(nameof(SlideDisplayTime), typeof(int), typeof(QueryDialog));

		string SlidesQuery { get { return (string)GetValue(SlidesQueryProperty); } set { SetValue(SlidesQueryProperty, value); } }
		string SlidesSize { get { return (string)GetValue(SlidesSizeProperty); } set { SetValue(SlidesSizeProperty, value); } }
		int SlideDisplayTime { get { return (int)GetValue(SlideDisplayTimeProperty); } set { SetValue(SlideDisplayTimeProperty, value); } }

		readonly NeoPlayerWindow neoPlayerWindow;
		public QueryDialog(NeoPlayerWindow neoPlayerWindow)
		{
			this.neoPlayerWindow = neoPlayerWindow;
			InitializeComponent();
			DataContext = this;
			SlidesQuery = neoPlayerWindow.SlidesQuery;
			SlidesSize = neoPlayerWindow.SlidesSize;
			SlideDisplayTime = neoPlayerWindow.SlideDisplayTime;
		}

		void OnOKClick(object sender, RoutedEventArgs e)
		{
			neoPlayerWindow.SlidesQuery = SlidesQuery;
			neoPlayerWindow.SlidesSize = SlidesSize;
			neoPlayerWindow.SlideDisplayTime = SlideDisplayTime;
			DialogResult = true;
		}
	}
}

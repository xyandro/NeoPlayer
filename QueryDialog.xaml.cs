using System.Windows;

namespace NeoRemote
{
	partial class QueryDialog
	{
		static DependencyProperty SlidesQueryProperty = DependencyProperty.Register(nameof(SlidesQuery), typeof(string), typeof(QueryDialog));
		static DependencyProperty SlidesSizeProperty = DependencyProperty.Register(nameof(SlidesSize), typeof(string), typeof(QueryDialog));
		static DependencyProperty SlideDisplayTimeProperty = DependencyProperty.Register(nameof(SlideDisplayTime), typeof(int), typeof(QueryDialog));

		string SlidesQuery { get { return (string)GetValue(SlidesQueryProperty); } set { SetValue(SlidesQueryProperty, value); } }
		string SlidesSize { get { return (string)GetValue(SlidesSizeProperty); } set { SetValue(SlidesSizeProperty, value); } }
		int SlideDisplayTime { get { return (int)GetValue(SlideDisplayTimeProperty); } set { SetValue(SlideDisplayTimeProperty, value); } }

		readonly Actions actions;
		public QueryDialog(Actions actions)
		{
			this.actions = actions;
			InitializeComponent();
			DataContext = this;
			SlidesQuery = actions.SlidesQuery;
			SlidesSize = actions.SlidesSize;
			SlideDisplayTime = actions.SlideDisplayTime;
		}

		void OnOKClick(object sender, RoutedEventArgs e)
		{
			actions.SlidesQuery = SlidesQuery;
			actions.SlidesSize = SlidesSize;
			actions.SlideDisplayTime = SlideDisplayTime;
			DialogResult = true;
		}
	}
}

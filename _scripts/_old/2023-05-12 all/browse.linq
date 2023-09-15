<Query Kind="Program">
  <Reference>C:\Dev_Nuget\Libs\DynaServe\Libs\DynaServeLib\bin\Debug\net7.0\DynaServeLib.dll</Reference>
  <Reference>C:\Dev_Nuget\Libs\ImdbLib\ImdbLib\bin\Debug\net7.0\ImdbLib.dll</Reference>
  <Namespace>static DynaServeLib.Nodes.Ctrls</Namespace>
  <Namespace>PowRxVar</Namespace>
  <Namespace>DynaServeLib</Namespace>
  <Namespace>DynaServeLib.Serving.FileServing.StructsEnum</Namespace>
</Query>

#load ".\browse-libs\base-ui"
#load ".\browse-libs\user-data"
#load ".\browse-libs\user-data-structs"
#load ".\browse-libs\movie-data"
#load ".\browse-ui\1_layout\layout"
#load ".\browse-ui\3_movies\1_movies-thumbs\movies-thumbs"
#load ".\browse-ui\4_pane-toggler\pane-toggler"
#load ".\browse-ui\5_panes\1_filter-pane\filter-pane"


/*
TODO:
=====
	x prevent adding the same PropFilter multiple times in the same Filter (they're considered Equals by the EditList - not great)

*/

void Main()
{
	var userData = new UserDataVars(testData: false).D(D);
	var movieData = new MovieVars(userData.ComboFilters, movieOpt =>
	{
		movieOpt.UseTestData = false;
		//movieOpt.ShowOnlyOneMovie = true;
	}).D(D);
	var panesVisibleVars = new PanesVisibleVars().D(D);

	Serv.Start(
		opt =>
		{
			opt.BaseConfig();
			/*opt.Serve(FCat.Css,
				"1_layout",
				"1_movies-thumbs",
				"4_pane-toggler",
				"1_filter-pane",
				"1_dlg-input"
			);*/
			opt.SearchFolder = Path.GetDirectoryName(Util.CurrentQueryPath);
			opt.ServeFolder("0_css-common", FCat.Css);
			opt.ServeFolder("1_layout", FCat.Css);
			opt.ServeFolder("3_movies", FCat.Css);
			opt.ServeFolder("4_pane-toggler", FCat.Css);
			opt.ServeFolder("5_panes", FCat.Css);
		},

		UI_Layout.Make(
			mainOverlayNodes: UI_FilterPane.Make(userData, panesVisibleVars.FltOpen),
			headerNodes: UI_MoviesThumbs.MakeHeader(movieData),
			mainNodes: UI_MoviesThumbs.MakeThumbs(movieData.Movies),
			asideNodes: UI_PaneToggler.Make(panesVisibleVars)
		)
	);
}


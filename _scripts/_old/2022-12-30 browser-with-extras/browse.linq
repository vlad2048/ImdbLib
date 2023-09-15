<Query Kind="Program">
  <Reference>C:\Dev_Nuget\Libs\DynaServe\Libs\DynaServeLib\bin\Debug\net7.0\DynaServeLib.dll</Reference>
  <Reference>C:\Dev_Nuget\Libs\ImdbLib\ImdbLib\bin\Debug\net7.0\ImdbLib.dll</Reference>
  <Namespace>static DynaServeLib.Nodes.Ctrls</Namespace>
  <Namespace>PowRxVar</Namespace>
  <Namespace>DynaServeLib</Namespace>
</Query>

#load ".\browse-libs\base-ui"
#load ".\browse-libs\user-data"
#load ".\browse-libs\movie-data"
#load ".\browse-ui\1_layout\layout"
#load ".\browse-ui\3_movies\1_movies-thumbs\movies-thumbs"
#load ".\browse-ui\4_pane-toggler\pane-toggler"
#load ".\browse-ui\5_panes\1_filter-pane\filter-pane"
#load ".\browse-ui\10_ctrls\1_dlg-input\dlg-input"


/*

browse-ui

	0_css-common

	1_layout
	
	2_header

	3_movies
		1_movies-thumbs
		2_movies-list

	4_pane-toggler

	5_panes
		1_filter-pane
		2_tags-pane
		3_opts-pane
		4_log-pane

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
		MkOpt(
			"1_layout",
			"1_movies-thumbs",
			"4_pane-toggler",
			"1_filter-pane",
			"1_dlg-input"
		),

		UI_Layout.Make(
			mainOverlayNodes: UI_FilterPane.Make(userData, panesVisibleVars.FltOpen),
			headerNodes: UI_MoviesThumbs.MakeHeader(movieData),
			mainNodes: UI_MoviesThumbs.MakeThumbs(movieData.Movies),
			asideNodes: UI_PaneToggler.Make(panesVisibleVars)
		)
	);
}


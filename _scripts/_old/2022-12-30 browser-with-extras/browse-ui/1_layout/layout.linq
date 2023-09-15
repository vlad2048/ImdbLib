<Query Kind="Program">
  <Reference>C:\Dev_Nuget\Libs\DynaServe\Libs\DynaServeLib\bin\Debug\net7.0\DynaServeLib.dll</Reference>
  <Namespace>DynaServeLib</Namespace>
  <Namespace>static DynaServeLib.Nodes.Ctrls</Namespace>
  <Namespace>DynaServeLib.Nodes</Namespace>
  <Namespace>PowBasics.CollectionsExt</Namespace>
</Query>

#load "..\..\browse-libs\base-ui"

void Main()
{
	Serv.Start(
		MkOpt(
			"1_layout"
		),
		
		UI_Layout.Make()
	);
}


public static class UI_Layout
{
	public static HtmlNode[] Make(
		HtmlNode[]? headerNodes = null,
		HtmlNode[]? mainNodes = null,
		HtmlNode[]? mainOverlayNodes = null,
		HtmlNode[]? asideNodes = null,
		HtmlNode[]? footerNodes = null
	)
	{
		headerNodes ??= Div().Txt("Header");
		mainNodes ??= Enumerable.Range(0, 30).Select(e => Div().Txt($"item_{e}")).ToArray();
		asideNodes ??= Div().Txt("Aside");
		footerNodes ??= Div().Txt("Footer");
		return new[]
		{
			THeader().Wrap(headerNodes),

			Div("main-div").Wrap(
				Div("main-content").Wrap(
					mainOverlayNodes switch
					{
						not null =>
							mainOverlayNodes
							.Append(
								TMain().Wrap(mainNodes)
							),
						null =>
							new []
							{
								TMain().Wrap(mainNodes)
							}
					}
					/*ArrOpt(
					mainOverlayNodes switch
					{
						not null => TSection("main-overlay").Wrap(mainOverlayNodes),
						null => null
					},
					TMain().Wrap(mainNodes)
					)*/
				),

				TAside().Wrap(asideNodes)
			),

			TFooter().Wrap(footerNodes)
		};
	}
}
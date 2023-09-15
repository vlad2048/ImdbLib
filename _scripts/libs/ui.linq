<Query Kind="Program">
  <Reference>C:\Dev_Nuget\Libs\LINQPadHero\LINQPadHero\bin\Debug\net7.0\LINQPadHero.dll</Reference>
  <Namespace>LINQPadHero</Namespace>
  <Namespace>PowRxVar</Namespace>
  <Namespace>LINQPadHero.UserPrefsCode</Namespace>
  <Namespace>LINQPad.Controls</Namespace>
</Query>

void Main()
{
	
}


public class UserPrefs
{
	public int Num { get; set; }
}

public static IRoDispBase D = null!;
public static IUserPrefsLogic<UserPrefs> Prefs = null!;

void OnStart()
{
	D = Hero.InitD();
	Prefs = Hero.InitUserPrefs<UserPrefs>("movie-browser");
}


public static class UISpaceExt
{
	public static C WhenClick<C>(this C ctrl, Action action) where C : Control
	{
		ctrl.Styles["cursor"] = "pointer";
		ctrl.Click += (_, _) => action();
		return ctrl;
	}
	
	public static C Horiz<C>(this C ctrl) where C : Control
	{
		ctrl.Styles["display"] = "flex";
		ctrl.Styles["flex-direction"] = "row";
		return ctrl;
	}
	
	public static C Vert<C>(this C ctrl) where C : Control
	{
		ctrl.Styles["display"] = "flex";
		ctrl.Styles["flex-direction"] = "column";
		return ctrl;
	}

	public static C Space<C>(this C ctrl) where C : Control
	{
		var gap = 5;
		ctrl.Styles["display"] = "flex";
		ctrl.Styles["column-gap"] = $"{gap}px";
		ctrl.Styles["align-items"] = "center";
		return ctrl;
	}
	public static C SpaceVert<C>(this C ctrl) where C : Control
	{
		var gap = 5;
		ctrl.Styles["display"] = "flex";
		ctrl.Styles["flex-direction"] = "column";
		ctrl.Styles["row-gap"] = $"{gap}px";
		return ctrl;
	}
	
	public static C BigSpace<C>(this C ctrl) where C : Control
	{
		var gap = 10;
		ctrl.Styles["display"] = "flex";
		ctrl.Styles["column-gap"] = $"{gap}px";
		ctrl.Styles["align-items"] = "center";
		return ctrl;
	}
	
	public static C SpaceBetween<C>(this C ctrl) where C : Control
	{
		ctrl.Styles["display"] = "flex";
		ctrl.Styles["justify-content"] = "space-between";
		ctrl.Styles["align-items"] = "center";
		return ctrl;
	}
	
	public static C SideBySide<C>(this C ctrl) where C : Control
	{
		var gap = 5;
		ctrl.Styles["display"] = "flex";
		ctrl.Styles["column-gap"] = $"{gap}px";
		ctrl.Styles["align-items"] = "start";
		return ctrl;
	}
}
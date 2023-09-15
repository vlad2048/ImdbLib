<Query Kind="Program">
  <Reference>C:\Dev_Nuget\Libs\ImdbLib\ImdbLib\bin\Debug\net7.0\ImdbLib.dll</Reference>
  <Namespace>ImdbLib</Namespace>
</Query>

void Main()
{
	var movies = MovieLoader.Load(opt => opt.DbgUseSmallDatasets = true);
	
	movies.Length.Dump();
}


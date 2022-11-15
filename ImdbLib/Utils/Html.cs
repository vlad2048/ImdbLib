using System.Xml.XPath;
using HtmlAgilityPack;
using PowMaybeErr;

namespace ImdbLib.Utils;

static class Html
{
	/*
	
	XPath cheatsheet
	================
	
		//section[@data-testid='Details']
	
	*/

	public static MaybeErr<HtmlNode> LoadFromUrl(string url) =>
		from str in HtmlUtils.GetPage(url, CancellationToken.None).Result
		from root in LoadFromString(str)
		select root;

	public static MaybeErr<HtmlNode> LoadFromFile(string file) => LoadFromString(File.ReadAllText(file));

	public static MaybeErr<HtmlNode> LoadFromString(string str) => WrapEx(() =>
	{
		var doc = new HtmlDocument();
		doc.LoadHtml(str);
		return doc.DocumentNode;
	});

	/// <summary>
	/// Gets the first node matching an xPath
	/// or return the parent if xPath is null
	/// </summary>
	public static MaybeErr<HtmlNode> QueryNode(this HtmlNode parent, string? xPath)
	{
		try
		{
			if (xPath == null) return MayErr.Some(parent);
			var node = parent.SelectSingleNode(xPath);
			return node switch
			{
				not null => MayErr.Some(node),
				null => MayErr.None<HtmlNode>($"Cannot find '{xPath}'")
			};
		}
		catch (Exception ex)
		{
			return MayErr.None<HtmlNode>($"{ex}");
		}
	}

	public static MaybeErr<HtmlNode[]> QueryNodes(this HtmlNode parent, string xPath)
	{
		try
		{
			var nodes = parent.SelectNodes(xPath);
			if (nodes == null)
				return MayErr.None<HtmlNode[]>("xpath: nodes == null");
			var nodeList = nodes.ToArray();
			if (nodeList.Length == 0)
				return MayErr.None<HtmlNode[]>("xpath: nodeList.Length == 0");
			return MayErr.Some(nodeList);
		}
		catch (XPathException ex)
		{
			return MayErr.None<HtmlNode[]>($"{ex}");
		}
	}
	
	public static MaybeErr<string[]> GetTextArray(this HtmlNode parent, string xPath) =>
		from nodes in parent.QueryNodes(xPath)
		select (
				from node in nodes
				select node.GetText()
			)
			.WhereSome()
			.ToArray();

	public static MaybeErr<string> GetText(this HtmlNode parent, string? xPath = null) =>
		from node in parent.QueryNode(xPath)
		select node.InnerText.Trim();

	public static MaybeErr<T> GetTextAs<T>(this HtmlNode parent, string? xPath = null) =>
		from node in parent.QueryNode(xPath)
		from val in node.InnerText.Trim().Convert<T>()
		select val;

	public static MaybeErr<string> GetAttr(this HtmlNode parent, string attrName, string? xPath = null) =>
		from subNode in parent.QueryNode(xPath)
		from attrValue in subNode.GetAttrInternal(attrName)
		select attrValue;

	public static MaybeErr<T> GetAttrAs<T>(this HtmlNode parent, string attrName, string? xPath = null) =>
		from subNode in parent.QueryNode(xPath)
		from attrValue in subNode.GetAttrInternal(attrName)
		from attrParsed in Convert<T>(attrValue)
		select attrParsed;

	private static MaybeErr<string> GetAttrInternal(this HtmlNode node, string attrName)
	{
		if (!node.Attributes.Contains(attrName))
			return MayErr.None<string>($"GetAttrInternal: missing attribute '{attrName}'");
		var attr = node.Attributes[attrName];
		return MayErr.Some(attr.Value);
	}

	private static MaybeErr<T> Convert<T>(this string str)
	{
		str = str.Trim();

		if (typeof(T) == typeof(int))
			return int.TryParse(str, out var res) switch
			{
				true => MayErr.Some((T)(object)res),
				false => MayErr.None<T>($"Could not parse '{str}' as an int")
			};

		if (typeof(T) == typeof(decimal))
			return decimal.TryParse(str, out var res) switch
			{
				true => MayErr.Some((T)(object)res),
				false => MayErr.None<T>($"Could not parse '{str}' as a decimal")
			};

		return MayErr.None<T>($"Do not know how to parse type: {typeof(T).Name}  str:'{str}'");
	}
}
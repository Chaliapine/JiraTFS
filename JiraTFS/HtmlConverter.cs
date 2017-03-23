using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace JiraTFS
{
	internal class HtmlConverter
	{
		public string Html2Wiki(string html)
		{
			var doc = new HtmlDocument();
			doc.LoadHtml(html);
			return Html2Wiki(doc.DocumentNode, "");
		}

		private void AppendConvertedStyleAttribute(HtmlAttribute attribute, StringBuilder preAttrs, StringBuilder postAttrs)
		{
			var attributeWithoutWhiteSpaces = attribute.Value.Replace(" ", string.Empty);
			string space = " ";
			if (attributeWithoutWhiteSpaces.Contains("font-weight:bold"))
			{
				preAttrs.Append(space + "*");
				postAttrs.Append(space + "*");
				space = "";
			}
			if (attributeWithoutWhiteSpaces.Contains("font-style:italic"))
			{
				preAttrs.Append(space + "_");
				postAttrs.Append(space + "_");
				space = "";
			}
			if (attributeWithoutWhiteSpaces.Contains("text-decoration:underline"))
			{
				preAttrs.Append(space + "+");
				postAttrs.Append(space + "+");
			}
			var postAttrsCharArray = postAttrs.ToString().ToCharArray();
			Array.Reverse(postAttrsCharArray);
			postAttrs.Clear();
			postAttrs.Append(postAttrsCharArray);
		}

		private string Html2Wiki(HtmlNode baseNode, string preParam, string currentWiki = "")
		{
			var builderFinalString = new StringBuilder();
			var builderTag = new StringBuilder();
			var builderTagEndParams = new StringBuilder();

			foreach (HtmlNode childNode in baseNode.ChildNodes)
			{
				if (childNode.Name == "span" && childNode.HasAttributes)
				{
					foreach (var attribute in childNode.Attributes)
					{
						if (attribute.Name == "style")
							AppendConvertedStyleAttribute(attribute, builderTag, builderTagEndParams);
					}

				}
				if (childNode.Name == "div")
				{
					builderTag.Append(Environment.NewLine);
				}
				if (childNode.Name == "li")
				{
					builderTag.Append(preParam);
					builderTagEndParams.Append(Environment.NewLine);
				}

				if (childNode.Name == "#text")
					builderTag.Append(childNode.InnerText.Replace("&nbsp;", " ").Trim(' '));

				if (childNode.Name == "br")
				{
					//builderTag.Append(Environment.NewLine);
					//builderTagEndParams.Append(Environment.NewLine);
				}
				string preSpace;
				string postSpace;
				if (builderFinalString.Length == 0)
				{
					preSpace = "uib".Contains(baseNode.Name) ? "" : " ";
					postSpace = preSpace;
				}
				else
				{
					preSpace = "uib".Contains(builderFinalString[builderFinalString.Length - 1]) ? "" : " ";
					postSpace = "uib".Contains(baseNode.Name) ? "" : " ";
				}

				if (childNode.Name == "u")
				{
					builderTag.Append(preSpace + "+");
					builderTagEndParams.Append("+" + postSpace);
				}

				if (childNode.Name == "i")
				{
					builderTag.Append(preSpace + "_");
					builderTagEndParams.Append("_" + postSpace);
				}
				if (childNode.Name == "b")
				{
					builderTag.Append(preSpace + "*");
					builderTagEndParams.Append("*" + postSpace);
				}
				if (childNode.HasChildNodes)
				{
					if (childNode.Name == "ul")
						builderTag.Append(Html2Wiki(childNode, preParam.TrimEnd(' ') + "* "));
					else if (childNode.Name == "ol")
						builderTag.Append(Html2Wiki(childNode, preParam.TrimEnd(' ') + "# "));
					else
						builderTag.Append(Html2Wiki(childNode, preParam));
				}
				builderTag.Append(builderTagEndParams);
				builderFinalString.Append(builderTag);
				builderTagEndParams.Clear();
				builderTag.Clear();
			}
			return builderFinalString.ToString();
		}
	}
}

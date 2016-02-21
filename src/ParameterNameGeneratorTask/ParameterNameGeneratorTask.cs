using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Xamarin.Android.Tools.Bytecode;
using System.Text.RegularExpressions;

namespace Tasks
{
	public class GenerateParameterNames : Task
	{
		[Required]
		public ITaskItem[] SourceJars { get; set; }

		[Required]
		public ITaskItem[] TransformFiles { get; set; }

		[Required]
		public ITaskItem GeneratedFile { get; set; }

		public ITaskItem ApiOutputFile { get; set; }

		public bool ForceMeaningfulParameterNames { get; set; }

		public override bool Execute()
		{
			Log.LogMessage("GenerateParameterNames Task");
			Log.LogMessage("  ApiOutputFile:  {0}", ApiOutputFile);
			Log.LogMessage("  GeneratedFile:  {0}", GeneratedFile);
			Log.LogMessage("  SourceJars:     {0}", string.Join(";", SourceJars.Select(x => x.ItemSpec)));
			Log.LogMessage("  TransformFiles: {0}", string.Join(";", TransformFiles.Select(x => x.ItemSpec)));

			// create the folder
			var dir = Path.GetDirectoryName(GeneratedFile.ItemSpec);
			if (!Directory.Exists(dir))
			{
				Directory.CreateDirectory(dir);
			}

			// read the jar files
			var classPath = new ClassPath()
			{
				ApiSource = "class-parse"
			};
			foreach (var jarFile in SourceJars)
			{
				if (ClassPath.IsJarFile(jarFile.ItemSpec))
				{
					classPath.Load(jarFile.ItemSpec);
				}
			}

			// build up the object tree
			var metadataElement = classPath.ToXElement();
			// remove any nodes that the user wants
			metadataElement = TransformXml(metadataElement);

			var packages = JavaPackage.Parse(metadataElement);
			var xParameters = packages.SelectMany(p => p.ToXElement(ForceMeaningfulParameterNames));

			// create the new xml document
			var xDoc = new XDocument(
				new XElement("metadata",
					xParameters.ToArray()));

			// make sure we don't have anything not in the api.xml
			RemoveIgnoredApiXmlMembers(xDoc);

			// save
			xDoc.Save(GeneratedFile.ItemSpec);

			return true;
		}

		private XElement TransformXml(XElement metadataElement)
		{
			var xDoc = new XDocument(metadataElement);
			foreach (var transform in TransformFiles)
			{
				var xTransform = XDocument.Load(transform.ItemSpec);

				// just remove all the nodes marked for removal
				var removeNodes = xTransform.Root.Elements("remove-node");
				foreach (var remove in removeNodes)
				{
					var path = remove.Attribute("path").Value;
					xDoc.XPathSelectElements(path).Remove();
				}
			}

			return xDoc.Root;
		}

		private void RemoveIgnoredApiXmlMembers(XDocument xDoc)
		{
			if (ApiOutputFile != null && File.Exists(ApiOutputFile.ItemSpec))
			{
				//load api.xml
				var api = XDocument.Load(ApiOutputFile.ItemSpec);

				// load the newly generated transform file
				foreach (var attr in xDoc.Root.Elements("attr").ToArray())
				{
					// if the parameter doesn't exist, then remove it
					var path = attr.Attribute("path").Value;
					if (!api.XPathSelectElements(path).Any())
					{
						attr.ReplaceWith(new XComment("not found in api.xml: " + attr.ToString()));
					}
				}
			}
		}

		private class JavaPackage
		{
			public string Name { get; set; }
			public JavaType[] Types { get; set; }

			public bool HasTypes => Types != null && Types.Length > 0;

			public static JavaPackage[] Parse(XElement metadataElement)
			{
				return metadataElement.Elements("package")
					.Select(xPackage => new JavaPackage
					{
						Name = xPackage.Attribute("name").Value,
						Types = xPackage.Elements()
							.Where(xType => JavaType.IsValid(xType.Name.LocalName))
							.Select(xType => new JavaType
							{
								Name = xType.Attribute("name").Value,
								Kind = xType.Name.LocalName,
								Visibility = xType.Attribute("visibility").Value,
								Members = xType.Elements()
									.Where(xMember => JavaMember.IsValid(xMember.Name.LocalName))
									.Select(xMember => new JavaMember
									{
										Name = xMember.Attribute("name").Value,
										Kind = xMember.Name.LocalName,
										Visibility = xMember.Attribute("visibility").Value,
										Parameters = xMember.Elements("parameter")
											.Select(xParameter => new JavaParameter
											{
												Name = xParameter.Attribute("name").Value,
												Type = xParameter.Attribute("type").Value
											})
											.ToArray()
									})
									.ToArray()
							})
							.ToArray()
					})
					.ToArray();
			}

			public IEnumerable<XNode> ToXElement(bool forceMeaningfulParameterNames)
			{
				const string packagePathTemplate = "/api/package[@name='{0}']";
				const string typePathTemplate = "/{0}[@name='{1}']";
				const string memberPathTemplate = "/{0}[@name='{1}' and count(parameter)={2} and {3}]";
				const string paramSeparator = " and ";
				const string paramPath = "parameter[{0}][@type='{1}']";
				const string parameterPathTemplate = "/parameter[{0}]";

				var package = this;
				if (package.HasTypes)
				{
					var packagePath = string.Format(packagePathTemplate, package.Name);
					foreach (var type in package.Types.Where(t => t.IsVisible && t.HasMembers))
					{
						// add some comments
						yield return new XComment(string.Format("{0} {1}.{2}", type.Kind, package.Name, type.Name));

						var typePath = packagePath + string.Format(typePathTemplate, type.Kind, type.Name);
						foreach (var member in type.Members.Where(m => m.IsVisible && m.HasParameters))
						{
							// make sure the parameter names are valid and meaningful
							member.EnsueValidAndUnique(forceMeaningfulParameterNames);

							// build the member selection path bit of the parameter
							var paramArray = new string[member.ParameterCount];
							for (int idx = 0; idx < member.ParameterCount; idx++)
							{
								var parameter = member.Parameters[idx];
								paramArray[idx] = string.Format(paramPath, idx + 1, parameter.Type);
							}
							var parameterString = string.Join(paramSeparator, paramArray);
							var memberPath = typePath + string.Format(memberPathTemplate, member.Kind, member.Name, member.ParameterCount, parameterString);

							// build the actual parameter path
							for (int idx = 0; idx < member.ParameterCount; idx++)
							{
								var eachPath = memberPath + string.Format(parameterPathTemplate, idx + 1);
								var parameter = member.Parameters[idx];

								// return the actual metadata entry
								yield return new XElement("attr", new[]
								{
									new XAttribute("path", eachPath),
									new XAttribute("name", "managedName")
								})
								{
									Value = parameter.ManagedName
								};
							}
						}
					}
				}
			}
		}

		private class JavaType
		{
			public string Name { get; set; }
			public string Kind { get; set; }
			public string Visibility { get; set; }
			public JavaMember[] Members { get; set; }

			public static bool IsValid(string kind) => kind == "class" || kind == "interface";
			public bool HasMembers => Members != null && Members.Length > 0;
			public bool IsVisible => Visibility == "public" || Visibility == "protected";
		}

		private class JavaMember
		{
			private static Regex genericTemplate = new Regex(
				@"<.{0,}>",
				RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

			private static Regex alphanumericTemplate = new Regex(
				@"[^\w_]",
				RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

			public string Name { get; set; }
			public string Kind { get; set; }
			public string Visibility { get; set; }
			public JavaParameter[] Parameters { get; set; }

			public static bool IsValid(string kind) => kind == "method" || kind == "constructor";
			public bool IsVisible => Visibility == "public" || Visibility == "protected";
			public int ParameterCount => Parameters.Length;
			public bool HasParameters => Parameters != null && Parameters.Length > 0;

			public void EnsueValidAndUnique(bool forceMeaningfulParameterNames)
			{
				var addedParamNames = new List<string>();
				for (int idx = 0; idx < ParameterCount; idx++)
				{
					var parameter = Parameters[idx];
					var managedName = parameter.Name;

					if (forceMeaningfulParameterNames)
					{
						// if the parameter name is generated, try harder
						var isGenerated =
							managedName.StartsWith("p") &&
							managedName.Length > 1 &&
							char.IsDigit(managedName[1]);
						if (isGenerated)
						{
							// remove generics part (eg: SomeType<T>)
							var type = genericTemplate.Replace(parameter.Type, string.Empty);
							// get the type as the parameter name
							type = type.Split(new[] { "." }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? "param";
							// change arrays
							if (type.EndsWith("[]"))
							{
								type = type.Replace("[]", "Array");
							}
							// remove invalid characters
							type = alphanumericTemplate.Replace(type, string.Empty);
							// make sure it is camel case
							managedName = type[0].ToString().ToLower() + type.Substring(1);
						}
					}

					// fix any bad C# parameter names
					if (!managedName.StartsWith("@"))
					{
						managedName = "@" + managedName;
					}

					// make sure the name is unique for this method
					var tmpName = managedName;
					int pi = 2;
					while (addedParamNames.Contains(tmpName))
					{
						tmpName = managedName + pi++;
					}
					addedParamNames.Add(tmpName);

					parameter.ManagedName = tmpName;
				}
			}
		}

		private class JavaParameter
		{
			public string Name { get; set; }
			public string Type { get; set; }

			public string ManagedName { get; set; }
		}
	}
}

using System.Reflection;
using NUnit.Framework;
using System.IO;
using System.Linq;
using Mono.Cecil;

namespace ParameterNameGeneratorTask.Tests
{
	[TestFixture]
	public sealed class ParameterNameGeneratorTests
	{
		private const string JarBindingDllName = "ParameterNameGeneratorTask.Tests.AndroidJar.dll";
		private const string AarBindingDllName = "ParameterNameGeneratorTask.Tests.AndroidAar.dll";

		private static string GetBindingDllPath(string dll)
		{
			var assembly = typeof(ParameterNameGeneratorTests).Assembly.Location;
			var dir = Path.GetDirectoryName(assembly);
			var path = Path.Combine(dir, dll);
			return path;
		}

		[Test]
		public void BoundJarMethodsShouldHaveParameterNames()
		{
			// Given
			var assembly = AssemblyDefinition.ReadAssembly(GetBindingDllPath(JarBindingDllName));
			var type = assembly.MainModule.GetType("ParameterNameGeneratorTask.Tests.AndroidJar.OkBuffer");

			// When
			var method = type.Methods
				.Where(m => m.Name == "Read" && m.Parameters.Count == 3)
				.Where(m =>
					m.Parameters[0].ParameterType.FullName == typeof(byte[]).FullName &&
					m.Parameters[1].ParameterType.FullName == typeof(int).FullName &&
					m.Parameters[2].ParameterType.FullName == typeof(int).FullName)
				.Single();
			var parameters = method.Parameters;

			// Then
			Assert.AreEqual("sink", parameters[0].Name);
			Assert.AreEqual("offset", parameters[1].Name);
			Assert.AreEqual("byteCount", parameters[2].Name);
		}

		[Test]
		public void BoundAarMethodsShouldHaveParameterNames()
		{
			// Given
			var assembly = AssemblyDefinition.ReadAssembly(GetBindingDllPath(AarBindingDllName));
			var type = assembly.MainModule.GetType("ParameterNameGeneratorTask.Tests.AndroidAar.CalendarCellView");

			// When
			var method = type.Methods
				.Where(m => m.Name == "SetHighlighted" && m.Parameters.Count == 1)
				.Where(m => m.Parameters[0].ParameterType.FullName == typeof(bool).FullName)
				.Single();
			var parameters = method.Parameters;

			// Then
			Assert.AreEqual("highlighted", parameters[0].Name);
		}
	}
}

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
			var type = assembly.MainModule.GetType("ParameterNameGeneratorTask.Tests.AndroidJar.MyExample");

			// When
			var method = type.Methods.Where(m => m.Name == "TestValid").Single();
			var parameters = method.Parameters;

			// Then
			Assert.AreEqual("text", parameters.Single().Name);
		}

		[Test]
		public void BoundJarMethodsWithMultipleParametersShouldHaveParameterNames()
		{
			// Given
			var assembly = AssemblyDefinition.ReadAssembly(GetBindingDllPath(JarBindingDllName));
			var type = assembly.MainModule.GetType("ParameterNameGeneratorTask.Tests.AndroidJar.MyExample");

			// When
			var method = type.Methods.Where(m => m.Name == "TestValidMultiple").Single();
			var parameters = method.Parameters;

			// Then
			Assert.AreEqual("text", parameters[0].Name);
			Assert.AreEqual("message", parameters[1].Name);
		}

		[Test]
		public void BoundJarMethodsWithKeywordParametersShouldHaveParameterNames()
		{
			// Given
			var assembly = AssemblyDefinition.ReadAssembly(GetBindingDllPath(JarBindingDllName));
			var type = assembly.MainModule.GetType("ParameterNameGeneratorTask.Tests.AndroidJar.MyExample");

			// When
			var method = type.Methods.Where(m => m.Name == "TestKeyword").Single();
			var parameters = method.Parameters;

			// Then
			Assert.AreEqual("stringParam", parameters.Single().Name);
		}

		[Test]
		public void BoundJarMethodsWithContextualKeywordParametersShouldHaveParameterNames()
		{
			// Given
			var assembly = AssemblyDefinition.ReadAssembly(GetBindingDllPath(JarBindingDllName));
			var type = assembly.MainModule.GetType("ParameterNameGeneratorTask.Tests.AndroidJar.MyExample");

			// When
			var method = type.Methods.Where(m => m.Name == "TestContextualKeyword").Single();
			var parameters = method.Parameters;

			// Then
			Assert.AreEqual("asyncParam", parameters.Single().Name);
		}

		[Test]
		public void BoundJarMethodsWithNoParameterNamesShouldHaveParameterNames()
		{
			// Given
			var assembly = AssemblyDefinition.ReadAssembly(GetBindingDllPath(JarBindingDllName));
			var type = assembly.MainModule.GetType("ParameterNameGeneratorTask.Tests.AndroidJar.MyExample");

			// When
			var method = type.Methods.Where(m => m.Name == "TestEmpty").Single();
			var parameters = method.Parameters;

			// Then
			Assert.AreEqual("stringParam", parameters.Single().Name);
		}

		[Test]
		public void BoundJarMethodsWithMultipleNoParameterNamesShouldHaveParameterNames()
		{
			// Given
			var assembly = AssemblyDefinition.ReadAssembly(GetBindingDllPath(JarBindingDllName));
			var type = assembly.MainModule.GetType("ParameterNameGeneratorTask.Tests.AndroidJar.MyExample");

			// When
			var method = type.Methods.Where(m => m.Name == "TestEmptyMultiple").Single();
			var parameters = method.Parameters;

			// Then
			Assert.AreEqual("stringParam", parameters[0].Name);
			Assert.AreEqual("stringParam2", parameters[1].Name);
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

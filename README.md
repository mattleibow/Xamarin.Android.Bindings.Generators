# Xamarin.Android Binding Generators

[![Build status][badge]][ci]

A MSBuild Task to make Xamarin.Android binding projects easier.

Currently, there is no nice way to set the parameter names for methods
when binding Xamarin.Android libraries. This tiny little NuGet adds a 
`.targets` file to the Xamarin.Android Binding project. The `.targets`
file adds a MSBui8ld task which will then read the `<InputJar>` and 
`<EmbeddedJar>` elements and automatically generate the transform
files needed to set the parameter names for all the bound methods. 

## Usage

Using this is very simple, just install the NuGet:

     PM> Install-Package Xamarin.Android.Bindings.Generators
     
Then, rebuild! That is all!

*As this is just used for binding projects, it shouldn't be installed
into Xamarin.Android app projects.*

[badge]: https://ci.appveyor.com/api/projects/status/ee9grjpxpb8dkc7v?svg=true
[ci]: https://ci.appveyor.com/project/mattleibow/xamarin-android-bindings-generators

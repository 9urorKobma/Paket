//<Expects status="Error" span="(3,1)" id="FS3217">Package resolution</Expects>

#r "paket: nuget SomeInvalidNugetPackage"

open XPlot.Plotly

Chart.Line [ 1 .. 10 ]
/// Contains methods to find outdated packages.
module Paket.FindOutdated

open Paket.Domain
open Paket.Logging

let private adjustVersionRequirements strict includingPrereleases (dependenciesFile: DependenciesFile) =
    //TODO: Anything we need to do for source files here?
    let newPackages =
        dependenciesFile.Packages
        |> List.map (fun p ->
            let v = p.VersionRequirement 
            let requirement,strategy =
                match strict,includingPrereleases with
                | true,true -> VersionRequirement.NoRestriction, p.ResolverStrategy
                | true,false -> v, p.ResolverStrategy
                | false,true -> 
                    match v with
                    | VersionRequirement(v,_) -> VersionRequirement(v,PreReleaseStatus.All), Max
                | false,false -> VersionRequirement.AllReleases, Max
            { p with VersionRequirement = requirement; ResolverStrategy = strategy})

    DependenciesFile(dependenciesFile.FileName, dependenciesFile.Options, dependenciesFile.Sources, newPackages, dependenciesFile.RemoteFiles)

/// Finds all outdated packages.
let FindOutdated(dependenciesFileName,strict,includingPrereleases) =
    let dependenciesFile =
        DependenciesFile.ReadFromFile dependenciesFileName
        |> adjustVersionRequirements strict includingPrereleases

    let resolution = dependenciesFile.Resolve(true)
    let resolvedPackages = resolution.ResolvedPackages.GetModelOrFail()
    let lockFile = LockFile.LoadFrom(dependenciesFile.FindLockfile().FullName)

    [for kv in lockFile.ResolvedPackages do
        let package = kv.Value
        match resolvedPackages |> Map.tryFind (NormalizedPackageName package.Name) with
        | Some newVersion -> 
            if package.Version <> newVersion.Version then 
                yield package.Name,package.Version,newVersion.Version
        | _ -> ()]

/// Prints all outdated packages.
let ShowOutdated(dependenciesFileName,strict,includingPrereleases) =
    let allOutdated = FindOutdated(dependenciesFileName,strict,includingPrereleases)
    if allOutdated = [] then
        tracefn "No outdated packages found."
    else
        tracefn "Outdated packages found:"
        for (PackageName name),oldVersion,newVersion in allOutdated do
            tracefn "  * %s %s -> %s" name (oldVersion.ToString()) (newVersion.ToString())
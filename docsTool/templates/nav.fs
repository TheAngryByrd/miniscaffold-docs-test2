module Nav

open System

open Fable.React
open Fable.React.Props

type NameOfArticle = string
type UrlPath = string

type TopLevelNav = {
    DocsRoot : IO.DirectoryInfo
    DocsPages : IO.FileInfo list
}

let normalizeText text =
    System.Text.RegularExpressions.Regex.Replace(text, @"[^0-9a-zA-Z\.]+", " ")

let normalizeStr =  normalizeText >> str

let navItem text link =
        li [
            Class "nav-item"
        ] [
            a [
                Class "nav-link"
                Href link
            ] [
                normalizeStr text
            ]
        ]

let dropDownNavMenu text items =
            li [ Class "nav-item dropdown" ][
                a [
                    Id (sprintf "navbarDropdown-%s"  text)
                    Href "#"
                    DataToggle "dropdown"
                    AriaHasPopup true
                    AriaExpanded false
                    Class "nav-link dropdown-toggle" ]
                    [ normalizeStr text ]
                ul [    HTMLAttr.Custom ("aria-labelledby", "dropdownMenu1")
                        Class "dropdown-menu border-0 shadow" ] items ]

let dropDownNavItem text link =
    li [
        Class "nav-item"
    ] [
        a [
            Class "dropdown-item"
            Href link
        ] [
            normalizeStr text
        ]
    ]
let dropdownSubMenu text items =
    li [ Class "dropdown-submenu" ] [
        a [ Id (sprintf "navbarDropdown-%s"  text)
            Href "#"
            Role "button"
            DataToggle "dropdown"
            AriaHasPopup true
            AriaExpanded false
            Class "dropdown-item dropdown-toggle" ] [
                normalizeStr text ]
        ul [
            HTMLAttr.Custom ("aria-labelledby", "dropdownMenu2")
            Class "dropdown-menu border-0 shadow" ] items
    ]

type NavTree =
| File of title:string * link:string
| Folder of title: string * NavTree list

let rec sortNavTree (navtree : NavTree list) =
    navtree
    |> List.map(fun navTree ->
        match navTree with
        | File (t,l) -> File (t,l)
        | Folder(title, nodes) -> Folder(title, sortNavTree nodes)
    )
    |> List.sortBy(fun navtree ->
        match navtree with
        | File(title,_) -> title
        | Folder(title, _) -> title
    )

let navTreeFromPaths (rootPath : IO.DirectoryInfo) (files : IO.FileInfo list) =
    let rec addPath subFilePath parts nodes =
        match parts with
        | [] -> nodes
        | hp :: tp ->
            addHeadPath subFilePath hp tp nodes
    and addHeadPath subFilePath (part : string) remainingParts (nodes : NavTree list)=
        match nodes with
        | [] ->
            if part.EndsWith("html") then
                File(IO.Path.GetFileNameWithoutExtension part, subFilePath)
            else
                Folder(part, addPath subFilePath remainingParts [])
            |> List.singleton
        | Folder(title, subnodes) :: nodes when title = part -> Folder(title, addPath subFilePath remainingParts subnodes ) :: nodes
        | hn :: tn -> hn :: addHeadPath subFilePath part remainingParts tn

    ([], files)
    ||> List.fold(fun state file ->
        let subFilePath = file.FullName.Replace(rootPath.FullName, "")
        let pathParts = subFilePath.Split(IO.Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries) |> Array.toList
        addPath subFilePath pathParts state
    )



let generateNavMenus (navTree : NavTree list) =
    let rec innerDo depth (navTree : NavTree list) =
        navTree
        |> List.map(fun nav ->
            match nav with
            | File (title, link) when depth = 0 -> navItem title link
            | File (title, link) -> dropDownNavItem title link
            | Folder (title, subtree) when depth = 0 ->
                innerDo (depth + 1) subtree
                |> dropDownNavMenu title
            | Folder (title, subtree) ->
                innerDo (depth + 1) subtree
                |> dropdownSubMenu title
        )
    innerDo 0 navTree



let generateNav (gitRepoName : string) (topLevelNav : TopLevelNav) =

    nav [
        Class "navbar navbar-expand-lg sticky-top navbar-dark bg-dark"
    ] [
        i [ Class "fa fa-car"] []
        a [
            Class "navbar-brand"
            Href "/index.html"
        ] [str (gitRepoName)]
        button [
            Class "navbar-toggler"
            Type "button"
            DataToggle "collapse"
            HTMLAttr.Custom("data-target","#navbarNav" )
            HTMLAttr.Custom("aria-controls","navbarNav" )
            HTMLAttr.Custom("aria-expanded","false" )
            HTMLAttr.Custom("aria-label","Toggle navigation" )
        ] [
            span [Class "navbar-toggler-icon"] []
        ]
        div [   Class "collapse navbar-collapse"
                Id "navbarNav" ] [
            ul [ Class "navbar-nav mr-auto" ] [
                yield! navTreeFromPaths topLevelNav.DocsRoot topLevelNav.DocsPages |> sortNavTree |> generateNavMenus
            ]
        ]
    ]




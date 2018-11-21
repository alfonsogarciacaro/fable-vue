module Vue

open Fable.Core
open Fable.Import
open Fable.Core.JsInterop

type Any = obj
type NotSet = interface end

type Store<'Getters,'State,'Msg> =
    [<Emit("$0.getters")>]
    abstract Getters: 'Getters

type Component<'StoreGetters,'StoreState,'StoreMsg,'Props,'State,'Msg> =
    [<Emit("$0")>]
    abstract State: 'State
    [<Emit("$0")>]
    abstract Props: 'Props
    [<Emit("$0.$store")>]
    abstract Store: Store<'StoreGetters,'StoreState,'StoreMsg>

type Update<'StoreGetters,'StoreState,'StoreMsg,'Props,'State,'Msg> =
    Component<'StoreGetters,'StoreState,'StoreMsg,'Props,'State,'Msg> -> 'State -> 'Msg -> 'State

let private Vue: obj = importDefault "vue"
let private mkMethod
        (update: Update<'StoreGetters,'StoreState,'StoreMsg,'Props,'State,'Msg>)
        (mkMsg: obj[]->obj): obj = importMember "./Util"

// Helpers to access Component fields without type annotations

let inline state (vueComponent: Component<'StoreGetters,'StoreState,'StoreMsg,'Props,'State,'Msg>) =
    vueComponent.State

let inline props (vueComponent: Component<'StoreGetters,'StoreState,'StoreMsg,'Props,'State,'Msg>) =
    vueComponent.Props

let inline store (vueComponent: Component<'StoreGetters,'StoreState,'StoreMsg,'Props,'State,'Msg>) =
    vueComponent.Store

module internal Util =
    open System
    open FSharp.Reflection
    open Fable.Core.JsInterop
    open Fable.Core.DynamicExtensions
    open Fable.Import

    let (~%) kvs = createObj kvs

    let lowerFirst (str: string) =
        str.[0].ToString().ToLower() + str.[1..]

    let addKeyValue (key: string) (value: obj) (o: 'T): 'T =
        o.[key] <- value; o

    let addSubKeyValue (sub: string) (key: string) (value: obj) (o: 'T): 'T =
        if isNull o.[sub]
        then o.[sub] <- %[key ==> value]; o
        else o.[sub].[key] <- value; o

    let addState
            (msgType: Type)
            (init: unit->'Model)
            (update: Update<'StoreGetters,'StoreState,'StoreMsg,'Props,'State,'Msg>)
            (vueComponent: Component<'StoreGetters,'StoreState,'StoreMsg,'Props,NotSet,NotSet>) =
        let methodsObj =
            (obj(), FSharpType.GetUnionCases msgType) ||> Array.fold (fun acc msgCase ->
                let hasFields = msgCase.GetFields().Length > 0
                let mkMsg values =
                    let values = if hasFields then values else [||]
                    FSharpValue.MakeUnion(msgCase, values)
                 // Lower first letter to follow standards
                addKeyValue (lowerFirst msgCase.Name) (mkMethod update mkMsg) acc
            )
        JS.Object.assign(vueComponent, %[
            "data" ==> init
            "methods" ==> methodsObj
        ])

    // TODO: Add prop types, see https://vuejs.org/v2/guide/components-props.html#Prop-Types
    let addProps (propsType: Type) vueComponent =
        let props = FSharpType.GetRecordFields propsType
        JS.Object.assign(vueComponent, %[
            "props" ==> (props |> Array.map (fun p -> p.Name))
        ])

let inline makeComponent<'T> =
    obj() :?> Component<NotSet,NotSet,NotSet,NotSet,NotSet,NotSet>

let withName
        (name: string)
        (vueComponent: Component<'StoreGetters,'StoreState,'StoreMsg,'Props,'State,'Msg>) =
    vueComponent |> Util.addKeyValue "name" name

let inline withState
        (init: unit->'Model)
        (update: Update<'StoreGetters,'StoreState,'StoreMsg,'Props,'State,'Msg>)
        (vueComponent: Component<'StoreGetters,'StoreState,'StoreMsg,'Props,NotSet,NotSet>) =
    vueComponent
    |> Util.addState typeof<'Msg> init update
    :?> Component<'StoreGetters,'StoreState,'StoreMsg,'Props,'State,'Msg>

// let withStore
//     (storeDeclaration: 'StoreGetters * 'StoreState * 'StoreMsg -> unit)
//     (vueComponent: Component<'StoreGetters,'StoreState,'StoreMsg,NotSet,'State,'Msg>) =
//     vueComponent :?> Component<'StoreGetters,'StoreState,'StoreMsg,'Props,'State,'Msg>

let inline withProps
    (idProps: 'Props->'Props)
    (vueComponent: Component<'StoreGetters,'StoreState,'StoreMsg,NotSet,'State,'Msg>) =
    vueComponent
    |> Util.addProps typeof<'Props>
    :?> Component<'StoreGetters,'StoreState,'StoreMsg,'Props,'State,'Msg>

let withComputed
        (name: string, f: Component<'StoreGetters,'StoreState,'StoreMsg,'Props,'State,'Msg> -> 'T)
        (vueComponent: Component<'StoreGetters,'StoreState,'StoreMsg,'Props,'State,'Msg>) =
    vueComponent |> Util.addSubKeyValue "computed" name (fun () -> f jsThis)

let internal withComponentInternal
        (name: string)
        (importedComponent: obj)
        (vueComponent: Component<'StoreGetters,'StoreState,'StoreMsg,'Props,'State,'Msg>) =
    vueComponent |> Util.addSubKeyValue "components" name importedComponent

let inline withComponentFrom
        (dir: string)
        (name: string)
        (vueComponent: Component<'StoreGetters,'StoreState,'StoreMsg,'Props,'State,'Msg>) =
    withComponentInternal name (importDefault (dir + "/" + name + ".vue")) vueComponent

let inline withComponent
        (name: string)
        (vueComponent: Component<'StoreGetters,'StoreState,'StoreMsg,'Props,'State,'Msg>) =
    withComponentInternal name (importDefault ("./" + name + ".vue")) vueComponent

let mountApp (elSelector: string) (app: Component<_,_,_,_,_,_>): unit =
    let props = createObj [
        "el" ==> elSelector
        "render" ==> fun create -> create app
    ]
    createNew Vue props |> ignore

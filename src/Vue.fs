module Vue

open Fable.Core
open Fable.Import
open Fable.Core.JsInterop

[<Import("default", from="vue/dist/vue.esm.js")>]
let private Vue: obj = jsNative

[<AbstractClass>]
type VueExtra<'Model>(category, key, value) =
    member __.Category: string = category
    member __.Key: string = key
    member __.Value: obj = value

type IVueComponent =
    interface end

type IVue =
    interface end

type VueComputed<'T, 'Model>(name: string, compute: 'Model->'T) =
    inherit VueExtra<'Model>("computed", name, compute)

type VueComponents<'T>(values: seq<string * IVueComponent>) =
    inherit VueExtra<'T>("components", "values", values)

type VueTemplate<'T>(template: string) =
    inherit VueExtra<'T>("template", "template", template)

let computed name (compute: 'Model -> 'T) =
    VueComputed(name, compute)

let components (values: seq<string * IVueComponent>) =
    VueComponents<'T>(values)

let template (content: string) =
    VueTemplate<'T>(content)

module internal Internal =
    open System
    open FSharp.Reflection

    let mkMethod (update: IVue -> 'Props -> 'Model -> 'Msg -> 'Model)
                 (mkProps: obj -> obj)
                 (mkModel: obj -> obj)
                 (mkMsg: obj[] -> obj): obj = importMember "./Util"

    let lowerFirst (str: string) =
        str.[0].ToString().ToLower() + str.[1..]

    let mkComponent (propsType: Type)
                    (modelType: Type)
                    (msgType: Type)
                    (init: unit -> 'Model)
                    (update: IVue -> 'Props -> 'Model -> 'Msg -> 'Model)
                    (extra: VueExtra<'Model> seq) =

        let mkMakeRecordFn typ =
            let fields = FSharpType.GetRecordFields typ
            let fieldsDic = fields |> Array.mapi (fun i fi -> fi.Name, i) |> dict
            fields, fun (fieldObj: obj)  ->
                let values = Array.zeroCreate fields.Length
                for (KeyValue(k, i)) in fieldsDic do
                    values.[i] <- fieldObj?(k)
                FSharpValue.MakeRecord(typ, values)

        let props, mkProps = mkMakeRecordFn propsType
        let _, mkModel = mkMakeRecordFn modelType

        let methodsObj = obj()
        let computed = obj()
        let childComponents = obj()

        let msgCases = FSharpType.GetUnionCases msgType
        for msgCase in msgCases do
            // REVIEW: Lowering first letter to follow Vue.js standards for methods
            let k = lowerFirst msgCase.Name
            let hasFields = msgCase.GetFields().Length > 0
            methodsObj?(k) <- mkMethod update mkProps mkModel (fun values ->
                let values = if hasFields then values else [||]
                FSharpValue.MakeUnion(msgCase, values))

        let mutable template = "<template></template>"

        for kv in extra do
            if kv.Category = "computed" then
                computed?(kv.Key) <- fun () ->
                    kv.Value $ (mkModel jsThis)

            if kv.Category = "template" then
                template <- unbox<string> kv.Value

            if kv.Category = "components" then
                for (name, child) in unbox<seq<string * IVueComponent>> kv.Value do
                    childComponents?(name) <- child

        createObj [
            // TODO: Add prop types, see https://vuejs.org/v2/guide/components-props.html#Prop-Types
            "props" ==> (props |> Array.map (fun p -> p.Name))
            "data" ==> init
            "methods" ==> methodsObj
            "template" ==> template
            "computed" ==> computed
            "components" ==> childComponents
        ] :?> IVueComponent

let inline componentBuilder
                (init: unit -> 'Model)
                (update: IVue -> 'Props -> 'Model -> 'Msg -> 'Model)
                (extra: VueExtra<'Model> seq) =
    Internal.mkComponent typeof<'Props> typeof<'Model> typeof<'Msg> init update extra

let registerComponent (name: string) (component': IVueComponent): unit =
    Vue?``component``(name, component')

let mountApp (elSelector: string) (extra: VueExtra<'Model> seq): unit =
    let childComponents = obj()
    let mutable template = "<template></template>"

    for kv in extra do
        if kv.Category = "template" then
            template <- unbox<string> kv.Value

        if kv.Category = "components" then
            for (name, child) in unbox<seq<string * IVueComponent>> kv.Value do
                childComponents?(name) <- child

    let app = createObj [
        "name" ==> "App"
        "template" ==> template
        "components" ==> childComponents
    ]

    let props = createObj [
        "el" ==> elSelector
        "render" ==> fun create -> create app
    ]

    createNew Vue props |> ignore
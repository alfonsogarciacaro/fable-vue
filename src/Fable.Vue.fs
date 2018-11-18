module Fable.Vue

open Fable.Core
open Fable.Import
open Fable.Core.JsInterop

[<Import("default", from="vue/dist/vue.esm.js")>]
let Vue: obj = jsNative

[<AbstractClass>]
type VueExtra<'Model>(category, key, value) =
    member __.Category: string = category
    member __.Key: string = key
    member __.Value: obj = value

type VueComputed<'T, 'Model>(name: string, compute: 'Model->'T) =
    inherit VueExtra<'Model>("computed", name, compute)

type IVueComponent =
    interface end

module internal Internal =
    open System
    open FSharp.Reflection

    let mkMethod<'Model, 'Msg>
        (update: 'Model -> 'Msg -> 'Model)
        (mkModel: obj -> obj)
        (mkMsg: obj[] -> obj): obj = importMember "./Util"

    let lowerFirst (str: string) =
        str.[0].ToString().ToLower() + str.[1..]

    let mkComponent<'Model, 'Msg>
            (modelType: Type)
            (msgType: Type)
            (template: string)
            (init: unit -> 'Model)
            (update: 'Model -> 'Msg -> 'Model)
            (extra: VueExtra<'Model> seq) =
        let modelFields = FSharpType.GetRecordFields modelType
        let fieldsDic = modelFields |> Array.mapi (fun i fi -> fi.Name, i) |> dict
        let mkModel (fieldObj: obj) =
            let values = Array.zeroCreate modelFields.Length
            for (KeyValue(k, i)) in fieldsDic do
                values.[i] <- fieldObj?(k)
            FSharpValue.MakeRecord(modelType, values)
        let msgCases = FSharpType.GetUnionCases msgType
        let methodsObj = obj()
        for msgCase in msgCases do
            // REVIEW: Lowering first letter to follow Vue.js standards for methods
            let k = lowerFirst msgCase.Name
            // TODO: Check if the union case has no argument to prevent errors
            // when Vue passes the event as argument
            methodsObj?(k) <- mkMethod update mkModel (fun values ->
                FSharpValue.MakeUnion(msgCase, values))

        let computed = obj()
        for kv in extra do
            if kv.Category = "computed" then
                computed?(kv.Key) <- fun () ->
                    kv.Value $ (mkModel jsThis)

        // TODO: Mark all record fields as props or let/make user select them?
        createObj [
            "data" ==> init
            "template" ==> template
            "methods" ==> methodsObj
            "computed" ==> computed
        ] :?> IVueComponent

let inline componentBuilder<'Model, 'Msg>
                (template: string)
                (init: unit -> 'Model)
                (update: 'Model -> 'Msg -> 'Model)
                (extra: VueExtra<'Model> seq) =
    Internal.mkComponent typeof<'Model> typeof<'Msg> template init update extra

let registerComponent (name: string) (component': IVueComponent): unit =
    Vue?``component``(name, component')

let mountApp (elSelector: string): unit =
    createNew Vue (createObj["el" ==> elSelector]) |> ignore
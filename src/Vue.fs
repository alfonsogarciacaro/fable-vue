module Vue

open Fable.Core
open Fable.Import
open Fable.Core.JsInterop

[<Import("default", from="vue/dist/vue.esm.js")>]
let private Vue: obj = jsNative

[<AbstractClass>]
type VueExtra(category, key, value) =
    member __.Category: string = category
    member __.Key: string = key
    member __.Value: obj = value

type IVueComponent =
    interface end

type IVue<'Props> =
    abstract props: 'Props

type private VueProxy<'Props>(vue: obj, mkProps: obj->obj) =
    let mutable propsCache: 'Props option = None
    interface IVue<'Props> with
        member __.props =
            match propsCache with
            | Some props -> props
            | None ->
                let props = mkProps(vue) :?> 'Props
                propsCache <- Some props
                props

type VueExtras(values: VueExtra list) =
    inherit VueExtra("extras", "extras", values)

type VueComputed<'T, 'Model>(name: string, compute: 'Model->'T) =
    inherit VueExtra("computed", name, compute)

type VueComponent(name: string, value: IVueComponent) =
    inherit VueExtra("components", name, value)

type VueTemplate(template: string) =
    inherit VueExtra("template", "template", template)

type VueName(name: string) =
    inherit VueExtra("name", "name", name)

let computed (name, compute: 'Model -> 'T) =
    VueComputed(name, compute)

let components (values: seq<string * IVueComponent>) =
    values |> Seq.map (fun v -> VueComponent v :> VueExtra) |> Seq.toList |> VueExtras

let inline importComponent name =
    VueComponent(name, importDefault ("./" + name + ".vue"))

let name value =
    VueName(value)

let template (content: string) =
    VueTemplate(content)

module internal Internal =
    open System
    open FSharp.Reflection

    let mkMethod (update: IVue<'Props> -> 'Model -> 'Msg -> 'Model)
                 (mkVueProxy: obj -> obj)
                 (mkModel: obj -> obj)
                 (mkMsg: obj[] -> obj): obj = importMember "./Util"

    let lowerFirst (str: string) =
        str.[0].ToString().ToLower() + str.[1..]

    type ExtraAccumulator =
        { components: obj
          name: string
          template: string
          other: Map<string, obj> }
        static member Empty =
            { components = obj()
              name = ""
              template = ""
              other = Map.empty }

    let addKeyValue k v (o: obj) =
        o?(k) <- v; o

    let rec resolveExtras f (acc: ExtraAccumulator) (extra: VueExtra seq) =
        (acc, extra) ||> Seq.fold (fun acc kv ->
            match kv.Category with
            | "extras" -> resolveExtras f acc !!kv.Value
            | "name" -> { acc with name = !!kv.Value }
            | "template"-> { acc with template = !!kv.Value }
            | "components" -> { acc with components = addKeyValue kv.Key kv.Value acc.components }
            | category -> f acc kv category)

    let addExtra key (value: obj) =
        match value with
        | :? string as v -> if String.IsNullOrEmpty v then None else Some(key ==> v)
        | :? (obj array) as v -> if v.Length = 0 then None else Some(key ==> v)
        | v -> if JS.Object.keys(v).Count = 0 then None else Some(key ==> v)

    let mkComponent (propsType: Type)
                    (modelType: Type)
                    (msgType: Type)
                    (init: unit -> 'Model)
                    (update: IVue<'Props> -> 'Model -> 'Msg -> 'Model)
                    (extra: VueExtra seq) =

        let mkMakeRecordFn (typ: Type) =
            if typ.FullName = typeof<obj>.FullName
            then [||], Unchecked.defaultof<_>
            else
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
        for msgCase in FSharpType.GetUnionCases msgType do
            // REVIEW: Lowering first letter to follow Vue.js standards for methods
            let k = lowerFirst msgCase.Name
            let hasFields = msgCase.GetFields().Length > 0
            methodsObj?(k) <- mkMethod update (fun v -> upcast VueProxy(v, mkProps)) mkModel (fun values ->
                let values = if hasFields then values else [||]
                FSharpValue.MakeUnion(msgCase, values))

        let extra =
            ({ ExtraAccumulator.Empty with other = Map ["computed", obj()] }, extra)
            ||> resolveExtras (fun acc e -> function
                | "computed" ->
                    let computed = acc.other.["computed"] |> addKeyValue e.Key (fun () ->
                        e.Value $ (mkModel jsThis))
                    { acc with other = Map.add "computed" computed acc.other }
                | _ -> acc)

        [
            Some("data" ==> init)
            Some("methods" ==> methodsObj)
            addExtra "computed" extra.other.["computed"]
            // TODO: Add prop types, see https://vuejs.org/v2/guide/components-props.html#Prop-Types
            addExtra "props" (props |> Array.map (fun p -> p.Name))
            addExtra "name" extra.name
            addExtra "template" extra.template
            addExtra "components" extra.components
        ]
        |> List.choose id |> createObj :?> IVueComponent

let inline componentBuilder
                (init: unit -> 'Model)
                (update: IVue<'Props> -> 'Model -> 'Msg -> 'Model)
                (extra: VueExtra seq) =
    Internal.mkComponent typeof<'Props> typeof<'Model> typeof<'Msg> init update extra

let registerComponent (name: string) (component': IVueComponent): unit =
    Vue?``component``(name, component')

let stateless (extra: VueExtra seq) =
    let extra =
        (Internal.ExtraAccumulator.Empty, extra)
        ||> Internal.resolveExtras (fun acc _ _ -> acc)
    [
        Internal.addExtra "name" extra.name
        Internal.addExtra "template" extra.template
        Internal.addExtra "components" extra.components
    ]
    |> List.choose id |> createObj :?> IVueComponent

let mountApp (elSelector: string) (app: IVueComponent): unit =

    let props = createObj [
        "el" ==> elSelector
        "render" ==> fun create -> create app
    ]

    createNew Vue props
    |> ignore

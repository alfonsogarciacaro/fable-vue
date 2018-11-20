module DraggableHeader

open Fable.Core
open Fable.Core.JsInterop
open Fable.Import

type Point = { x: float; y: float }

type IDynamics =
    abstract animate: Point * Point * obj -> unit
    abstract spring: obj
    abstract bounce: obj
    abstract forceWithGravity: obj
    abstract gravity: obj
    abstract easeInOut: obj
    abstract easeIn: obj
    abstract easeOut: obj
    abstract linear: obj
    abstract bezier: obj

let [<Global>] dynamics: IDynamics = jsNative

type Props =
    { title: string }

type Model =
    { dragging: bool
      current: Point
      start: Point }

let init () =
    { dragging = false
      current = { x=160.; y=160. }
      start = { x=0.; y=0. } }

let getPage (e: Browser.MouseEvent) =
    // Check if this is a touch event
    let te = e :?> Browser.TouchEvent
    if not(isNull te.changedTouches) then
        let te = te.changedTouches.[0]
        { x = te.pageX; y = te.pageY }
    else
        { x = e.pageX; y = e.pageY }

type Msg =
    | StartDrag of Browser.MouseEvent
    | OnDrag of Browser.MouseEvent
    | StopDrag

/// Small helper to create plain JS objects
let inline pojo x = createObj x

let update (_vue: Vue.IVue<Props>) state = function
    | StartDrag e ->
        { state with dragging = true
                     start = getPage e }
    | OnDrag e when state.dragging ->
        let page = getPage e
        let start = state.start
        let x = 160. + (page.x - start.x)
        // dampen vertical drag by a factor
        let dy = page.y - start.y
        let dampen = if dy > 0. then 1.5 else 4.
        let y = 160. + dy / dampen
        { state with current = { x = x; y = y } }
    | StopDrag when state.dragging ->
        dynamics.animate(state.current, {x=160.; y=160.}, pojo [
                "type" ==> dynamics.spring
                "duration" ==> 700
                "friction" ==> 280
            ])
        { state with dragging = false }
    | OnDrag _ | StopDrag -> state

let headerPath model =
    "M0,0 L320,0 320,160Q" + string model.current.x + "," + string model.current.y + " 0,160"

let contentPosition model =
    let dy = model.current.x - 160.
    let dampen = if dy > 0. then 2. else 4.
    pojo [
        "transform" ==> "translate3d(0," + string(dy/dampen) + "px,0)"
    ]

Vue.componentBuilder init update [
    Vue.computed (nameof2 headerPath)
    Vue.computed (nameof2 contentPosition)
] |> exportDefault
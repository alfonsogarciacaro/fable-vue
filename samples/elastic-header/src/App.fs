module App

open Fable.Core
open Fable.Core.JsInterop
open Fable.Import

let [<Global>] dynamics: obj = jsNative

type Point =
    { x: float; y: float }
    // static member Create(x, y) =
    //     { x=x; y=y }

type Model =
    { dragging: bool
      c: Point
      start: Point }

let init () =
    { dragging = false
      c = { x=160.; y=160. }
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
    | StopDrag of Browser.MouseEvent

let update state = function
    | StartDrag e ->
        { state with dragging = true
                     start = getPage e }
    | OnDrag e when state.dragging ->
        let page = getPage e
        let start = state.start
        let x = 160. + (page.x - start.x)
        // dampen vertical drag by a factor
        let dy = page.y - start.y
        let dampen = if dy > 0. then 0.5 else 4.
        let y = 160. + dy / dampen
        { state with c = { x=x; y=y } }
    | StopDrag _ when state.dragging ->
        dynamics?animate(state.c, {x=160.; y=160.}, createObj [
                "type" ==> dynamics?spring
                "duration" ==> 700
                "friction" ==> 280
            ])
        { state with dragging = false }
    | _ -> state

let headerPath model =
    "M0,0 L320,0 320,160Q" + string model.c.x + "," + string model.c.y + " 0,160"

let contentPosition model =
    let dy = model.c.x - 160.
    let dampen = if dy > 0. then 2. else 4.
    createObj [
        "transform" ==> "translate3d(0," + string(dy/dampen) + "px,0)"
    ]

open Fable.Vue

componentBuilder "#header-view-template" init update [
    VueComputed("headerPath", headerPath)
    VueComputed("contentPosition", contentPosition)
] |> registerComponent "draggable-header-view"

mountApp "#app"
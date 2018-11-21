module DraggableHeader

open Vue
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import

type Point =
    { x: float
      y: float }

type Props =
    { title: string }

type Model =
    { dragging: bool
      current: Point
      start: Point }

type Msg =
    | StartDrag of Browser.MouseEvent
    | OnDrag of Browser.MouseEvent
    | StopDrag

type IDynamics =
    abstract animate: Point * Point * obj -> unit
    abstract spring: obj

let [<Global>] dynamics: IDynamics = jsNative

let init () =
    { dragging = false
      current = { x=160.; y=160. }
      start = { x=0.; y=0. } }

/// Checks if this is a touch event
let getPage (e: Browser.MouseEvent) =
    let te = e :?> Browser.TouchEvent
    if not(isNull te.changedTouches) then
        let te = te.changedTouches.[0]
        { x = te.pageX; y = te.pageY }
    else
        { x = e.pageX; y = e.pageY }

/// Small helper to create plain JS objects
let inline pojo x = createObj x

let update _ state = function
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

let headerPath vue =
    let state = state vue: Model
    "M0,0 L320,0 320,160Q" + string state.current.x + "," + string state.current.y + " 0,160"

let contentPosition vue =
    let dy = (state vue: Model).current.x - 160.
    let dampen = if dy > 0. then 2. else 4.
    pojo [
        "transform" ==> "translate3d(0," + string(dy/dampen) + "px,0)"
    ]

makeComponent
|> withState init update
|> withProps (fun p -> p: Props)
|> withComputed (nameof2 headerPath)
|> withComputed (nameof2 contentPosition)
|> exportDefault
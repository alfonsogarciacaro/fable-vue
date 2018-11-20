module App

open Fable.Core.JsInterop

Vue.stateless [
    Vue.name "App"
    Vue.importComponent "DraggableHeader"
] |> exportDefault

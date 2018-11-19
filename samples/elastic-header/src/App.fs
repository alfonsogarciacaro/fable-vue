module App

open Fable.Core.JsInterop

let app = Vue.stateless [
    Vue.name "App"
    Vue.template (importAll "./App.html")
    Vue.components [
        "draggable-header-view", DraggableHeader.draggableHeader
    ]
]

Vue.mountApp "#root" app
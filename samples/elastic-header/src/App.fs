module App

open Fable.Core.JsInterop

Vue.mountApp "#root" [
    // child components
    Vue.components [
        "draggable-header-view", DraggableHeader.draggableHeader
    ]
    Vue.template (importAll "./App.html")
]

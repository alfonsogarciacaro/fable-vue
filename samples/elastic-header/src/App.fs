module App

open Fable.Core.JsInterop

let app = Vue.stateless [
    Vue.name "App"
    Vue.template (importDefault "./App.html")
    Vue.components [
        "draggable-header-view", importDefault "../components/DraggableHeader.vue"
    ]
]

Vue.mountApp "#root" app
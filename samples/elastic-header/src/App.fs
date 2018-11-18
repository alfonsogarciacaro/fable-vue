module App

open Fable.Core.JsInterop 

type Model = { Content : obj option }  
let init() = { Content = None }

type Msg = Msg of int
let update state = function 
    | Msg _ -> state

let app = Vue.componentBuilder init update [
    // child components
    Vue.components [
        "draggable-header-view", DraggableHeader.draggableHeader
    ]

    Vue.template (importAll "./App.html") 
]

Vue.mountApp "#root" app 
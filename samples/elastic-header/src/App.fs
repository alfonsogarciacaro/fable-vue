module App

open Vue
open Fable.Core.JsInterop

makeComponent
|> withName "App"
|> withComponent "DraggableHeader"
|> exportDefault

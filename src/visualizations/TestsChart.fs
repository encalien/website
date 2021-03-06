[<RequireQualifiedAccess>]
module TestsChart

open System
open Elmish
open Feliz
open Feliz.ElmishComponents

open Types
open Highcharts

type DisplayType =
    | Total
    | Regular
    | NsApr20
with
    static member all = [ Regular; NsApr20; Total; ]
    static member getName = function
        | Total -> "Skupaj"
        | Regular -> "Redno"
        | NsApr20 -> "Raziskava"

type State = {
    data: StatsData
    displayType: DisplayType
}

type Msg =
    | ChangeDisplayType of DisplayType

let init data : State * Cmd<Msg> =
    let state = {
        data = data
        displayType = Regular
    }
    state, Cmd.none

let update (msg: Msg) (state: State) : State * Cmd<Msg> =
    match msg with
    | ChangeDisplayType dt ->
        { state with displayType = dt }, Cmd.none

let renderChartOptions (state : State) =
    let className = "tests-chart"
    let scaleType = ScaleType.Linear

    let positiveTests (dp: StatsDataPoint) =
        match state.displayType with
        | Total     -> dp.Tests.Positive.Today.Value
        | Regular   -> dp.Tests.Regular.Positive.Today.Value
        | NsApr20   -> dp.Tests.NsApr20.Positive.Today.Value
    let negativeTests (dp: StatsDataPoint) =
        match state.displayType with
        | Total     -> dp.Tests.Performed.Today.Value - dp.Tests.Positive.Today.Value
        | Regular   -> dp.Tests.Regular.Performed.Today.Value - dp.Tests.Regular.Positive.Today.Value
        | NsApr20   -> dp.Tests.NsApr20.Performed.Today.Value - dp.Tests.NsApr20.Positive.Today.Value
    let percentPositive (dp: StatsDataPoint) =
        let positive = positiveTests dp
        let performed = positiveTests dp + negativeTests dp
        Math.Round(float positive / float performed * float 100.0, 2)

    let allYAxis = [|
        {|
            index = 0
            title = {| text = null |}
            labels = pojo {| format = "{value}" |}
            opposite = true
            visible = true
            max = None
        |}
        {|
            index = 1
            title = {| text = null |}
            labels = pojo {| format = "{value}%" |}
            opposite = false
            visible = true
            max = Some 15
        |}
    |]

    let allSeries = [
        yield pojo
            {|
                name = "Negativnih testov"
                ``type`` = "column"
                color = "#19aebd"
                yAxis = 0
                data = state.data |> Seq.filter (fun dp -> dp.Tests.Positive.Today.IsSome )
                    |> Seq.map (fun dp -> (dp.Date |> jsTime12h, negativeTests dp)) |> Seq.toArray
            |}
        yield pojo
            {|
                name = "Pozitivnih testov"
                ``type`` = "column"
                color = "#d5c768"
                yAxis = 0
                data = state.data |> Seq.filter (fun dp -> dp.Tests.Positive.Today.IsSome )
                    |> Seq.map (fun dp -> (dp.Date |> jsTime12h, positiveTests dp)) |> Seq.toArray
            |}
        yield pojo
            {|
                name = "Delež pozitivnih testov (%)"
                ``type`` = "line"
                color = "#665191"
                yAxis = 1
                data = state.data |> Seq.filter (fun dp -> dp.Tests.Positive.Today.IsSome )
                    |> Seq.map (fun dp -> (dp.Date |> jsTime12h, percentPositive dp)) |> Seq.toArray
            |}
    ]

    let baseOptions = Highcharts.basicChartOptions scaleType className
    {| baseOptions with
        yAxis = allYAxis
        series = List.toArray allSeries
        plotOptions = pojo
            {|
                series = {| stacking = "normal"; crisp = false; borderWidth = 0; pointPadding = 0; groupPadding = 0 |}
            |}

        legend = pojo
            {|
                enabled = true
                title = {| text = null |}
                align = "left"
                verticalAlign = "top"
                x = 60
                y = 30
                borderColor = "#ddd"
                borderWidth = 1
                layout = "vertical"
                floating = true
                backgroundColor = "#FFF"
            |}

        responsive = pojo
            {|
                rules =
                    [| {|
                        condition = {| maxWidth = 500 |}
                        chartOptions =
                            {|
                                legend = {| enabled = false |}
                                yAxis = [|
                                    {| labels = {| enabled = false |} |}
                                    {| labels = {| enabled = false |} |}
                                |]
                            |}
                    |} |]
            |}
    |}

let renderChartContainer (state : State) =
    Html.div [
        prop.style [ style.height 480 ]
        prop.className "highcharts-wrapper"
        prop.children [
            renderChartOptions state
            |> Highcharts.chartFromWindow
        ]
    ]

let renderSelector state (dt: DisplayType) dispatch =
    Html.div [
        let isActive = state.displayType = dt
        prop.onClick (fun _ -> ChangeDisplayType dt |> dispatch)
        prop.className [ true, "btn btn-sm metric-selector"; isActive, "metric-selector--selected" ]
        prop.text (DisplayType.getName dt) ]

let renderDisplaySelectors state dispatch =
    Html.div [
        prop.className "metrics-selectors"
        prop.children (
            DisplayType.all
            |> List.map (fun dt -> renderSelector state dt dispatch) ) ]

let render (state: State) dispatch =
    Html.div [
        renderChartContainer state
        renderDisplaySelectors state dispatch
    ]

let testsChart (props : {| data : StatsData |}) =
    React.elmishComponent("TestsChart", init props.data, update, render)

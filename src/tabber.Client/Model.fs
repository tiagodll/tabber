module tabber.Model

open Tabber.Shared.Model
open Tabber.Client

type Page =
    | Dashboard
    | Play of id: string
    | Edit of id: string
    | SignIn
    | SignUp


type Model =
    {
        page: Page
        error: string option
        state: State
        auth: Auth.Model
    }

and State = {
    play: PlayStateOrString
    dashboard: DashboardState
    edit: EditState option
}
and PlayState = {
    tab: Tab
    currentRiff: string
    riffCounter: int
    repCounter: int
}
and PlayStateOrString = 
    | PlayState of PlayState
    | Id of string

and DashboardState = {
    tabs: Tab list
    latestTabs: Tab list
}
and EditState = {
    tab: Tab
    tabText: string
}


type Message =
    | Init
    | SetPage of Page
    | Error of exn
    | ClearError
    | LatestTabsLoaded of string[]
    | TabsLoaded of string[]
    | TabAdded
    | DeleteTab of Tab
    | DeleteServerTab of Tab
    | TabServerDeleted
    | SetTabText of string
    | SaveTab
    | AddTab of Tab
    | MouseOverSeq of string
    | IncreaseCounter
    | DecreaseCounter
    | ResetCounter
    | Keydown of string
    | AuthMsg of Auth.Msg


let emptyTab = { id="new"; band="x"; title="y"; riffs=[]; sequence=[] }
let initModel =
    {
        page = Dashboard
        error = None
        state = { play = Id ""; edit = None; dashboard= { tabs = []; latestTabs = [] } }
        auth = Auth.init()
    }
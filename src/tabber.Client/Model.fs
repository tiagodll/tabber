module tabber.Model

open Tabber.Shared.Model

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
    }

and State = {
    play: PlayStateOrString
    dashboard: DashboardState
    edit: EditState option
    signedIn: User option
    signIn: SignInState option
    signUp: SignUpState option
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
and SignInState = {
    email: string
    password: string
    returnPage: Page
}
and SignUpState = {
    name: string
    email: string
    password: string
    password2: string
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
    | SetEmail of string
    | SetPassword of string
    | SendSignIn
    | RecvSignIn of User option
    | SendSignOut
    | RecvSignOut
    | Keydown of string


let emptySignInState returnPage = {email = ""; password = ""; returnPage = returnPage}
let emptySignUpState = {name=""; email = ""; password = ""; password2 = ""}
let emptyTab = { id="new"; band="x"; title="y"; riffs=[]; sequence=[] }
let initModel =
    {
        page = Dashboard
        error = None
        state = { play = Id ""; edit = None; dashboard= { tabs = []; latestTabs = [] }; signedIn = None; signIn = None; signUp = None }
    }
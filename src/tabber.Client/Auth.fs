module Tabber.Client.Auth

open Elmish
open Bolero.Remoting
open Bolero.Remoting.Client
open Tabber.Client.TabService
open Bolero
open Bolero.Html
open Tabber.Shared.Model

type Model = {
    signIn: SignInModel
    signUp: SignUpRequest
    signedInAs: User option
    error: string option
}
and SignInModel = {
    username: string
    password: string
    signInFailed: bool
}

let blankSignIn = {
    username = ""
    password = ""
    signInFailed = false
}
let blankSignUp = {
    title = ""
    url = ""
    password = ""
    password2 = ""
    signUpDone = false
}

let init() = 
  {
    signIn = blankSignIn
    signUp = blankSignUp
    error = None
    signedInAs = None
  }

type Msg =
    | SetUsername of string
    | SetPassword of string
    | SetSignUpTitle of string
    | SetSignUpUrl of string
    | SetSignUpPassword of string
    | SetSignUpPassword2 of string
    | GetSignedInAs
    | RecvSignedInAs of User option option
    | SendSignIn
    | RecvSignIn of User option
    | SendSignUp
    | RecvSignUp of string option
    | SendSignOut
    | RecvSignOut
    | AuthError of exn
    | ClearAuthError

let CreateId (n: string) =
  let rand = System.Random()
  let r = rand.NextDouble().ToString().[2..4]
  n.Split(" ")
  |> Array.map (fun s -> s.[0..1])
  |> (fun a -> Array.append a [| r |])
  |> String.concat ""

let update remote message model =
    match message with
    | SetUsername s ->
        //{ model with name = n; id = (CreateId n)}, Cmd.none
        { model with signIn={ model.signIn with username = s }}, Cmd.none
    | SetPassword s ->
        { model with signIn={ model.signIn with password = s }}, Cmd.none
    
    | SetSignUpTitle s ->
        { model with signUp={ model.signUp with title=s}}, Cmd.none
    | SetSignUpUrl s ->
        { model with signUp={ model.signUp with url = s}}, Cmd.none
    | SetSignUpPassword s ->
        { model with signUp={ model.signUp with password = s}}, Cmd.none
    | SetSignUpPassword2 s ->
        { model with signUp={ model.signUp with password2 = s}}, Cmd.none

    | GetSignedInAs ->
        model, Cmd.OfAuthorized.either remote.getUser () RecvSignedInAs AuthError
    | RecvSignedInAs user ->
        let u = match user with
                | None -> None
                | Some x -> x
        {model with signedInAs = u}, Cmd.none // onSignIn username
    
    | SendSignIn ->
        model, Cmd.OfAsync.either remote.signIn (model.signIn.username, model.signIn.password) RecvSignIn AuthError
    | RecvSignIn user ->
        { model with signedInAs = user; signIn={ model.signIn with signInFailed = Option.isNone user }}, Cmd.none // onSignIn username
    | SendSignOut ->
        model, Cmd.OfAsync.either remote.signOut () (fun () -> RecvSignOut) AuthError
    | RecvSignOut ->
        { model with signedInAs = None; signIn={ model.signIn with signInFailed = false }}, Cmd.none

    | SendSignUp ->
        model, Cmd.OfAsync.either remote.signUp model.signUp RecvSignUp AuthError
    | RecvSignUp error ->
        { model with error = error; signUp = {model.signUp with signUpDone = true }}, Cmd.none // onSignIn username

    | AuthError RemoteUnauthorizedException ->
        { model with error = Some "You have been logged out."; signedInAs = None}, Cmd.none
    | AuthError exn ->
        { model with error = Some exn.Message }, Cmd.none
    | ClearAuthError ->
        { model with error = None }, Cmd.none




let signInPage model dispatch =
    div[][
        h1 [attr.classes ["title"]] [text "Sign in"]
        form [on.submit (fun _ -> dispatch SendSignIn)] [
            div [attr.classes ["field"]] [
                label [attr.classes ["label"]] [text "email"]
                input [attr.classes ["input"]; bind.input.string model.signIn.username (fun s -> dispatch (SetUsername s))]
            ]
            div [attr.classes ["field"]] [
                label [attr.classes ["label"]] [text "Password"]
                input [attr.classes ["input"]; attr.``type`` "password"; bind.input.string model.signIn.password (fun s -> dispatch (SetPassword s))]
            ]
            div [attr.classes ["field"]] [
                input [attr.classes ["input"]; attr.``type`` "submit"; attr.value "Sign in"]
            ]
        ]
    ]


let signUpPage model dispatch =
    div[][
        h1 [attr.classes ["title"]] [text "Sign in"]
        form [on.submit (fun _ -> dispatch SendSignUp)] [
            div [attr.classes ["field"]] [
                label [attr.classes ["label"]] [text "Website title"]
                input [attr.classes ["input"]; bind.input.string model.signUp.title (dispatch << SetSignUpTitle)]
            ]
            div [attr.classes ["field"]] [
                label [attr.classes ["label"]] [text "Website Url"]
                input [attr.classes ["input"]; bind.input.string model.signUp.url (dispatch << SetSignUpUrl)]
            ]
            div [attr.classes ["field"]] [
                label [attr.classes ["label"]] [text "Password"]
                input [attr.classes ["input"]; attr.``type`` "password"; bind.input.string model.signUp.password (dispatch << SetSignUpPassword)]
            ]
            div [attr.classes ["field"]] [
                label [attr.classes ["label"]] [text "Repeat Password"]
                input [attr.classes ["input"]; attr.``type`` "password"; bind.input.string model.signUp.password2 (dispatch << SetSignUpPassword2)]
            ]
            div [attr.classes ["field"]] [
                input [attr.classes ["input"]; attr.``type`` "submit"; attr.value "Sign up"]
            ]
        ]
    ]
    
    
    
    

//open Elmish

//type Model = 
//  {
//    id: string
//    name: string
//    email: string
//    password: string
//    signInFailed: bool
//    signUpFailed: bool
//    signUpPage: bool
//  }

//let init() = 
//  {
//    id = ""
//    name = ""
//    email = ""
//    password = ""
//    signInFailed = false
//    signUpFailed = false
//    signUpPage = false
//  }

//type Msg =
//  | SetName of string
//  | SetEmail of string
//  | SetPassword of string
//  | SetPassword2 of string

//let CreateId (n: string) =
//  let rand = System.Random()
//  let r = rand.NextDouble().ToString().[2..4]
//  n.Split(" ")
//  |> Array.map (fun s -> s.[0..1])
//  |> (fun a -> Array.append a [| r |])
//  |> String.concat ""

//let update remote message model =
//    match message with
//    | SetName n ->
//        { model with name = n; id = (CreateId n)}, Cmd.none
//    | SetEmail s ->
//        { model with email = s }, Cmd.none
//    | SetPassword s ->
//        { model with password = s }, Cmd.none
//      // match s with
//      // | x -> //when x = model.password ->
//      //   { model with password = s }, Cmd.none
//      // | _ ->
//      //   model, Cmd.none // TODO: fix the password for sign up; return the error message
//    | SetPassword2 s ->
//        model, Cmd.none
//      // match s with
//      // | x -> //when x = model.password ->
//      //   model, Cmd.none
//      // | _ ->
//      //   model, Cmd.none // return the error message
module Tabber.Shared.Model

type Tab = {
    id: string
    band: string
    title: string
    riffs: Riff list
    sequence: Sequence list
}
and Riff = {
    name: string
    content: string   
}
and Sequence = {
    name: string
    reps: int   
}
and User = {
    email: string
    name: string
}
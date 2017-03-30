namespace Rui.Monads

module AsyncResult =

  let map f ar = async {
    let! r = ar
    return Result.map f r
  }

  let bind f ar = async {
    let! r = ar
    match r with
    | Ok v -> return! f v
    | Error e -> return e |> Error
  }

  let lift v = async {
      return v |> Result.Ok 
    }

  let rec traverse f ls =
    match ls with
    | [] -> lift []
    | x::xs -> 
        let hd = f x
        let rest = traverse f xs
        hd |> bind (fun h -> map (fun ls -> h :: ls) rest)

type AsyncResultBuilder() =
  member this.Return(v) = AsyncResult.lift v
  member this.ReturnFrom(v) = v
  member this.Bind(ar, f) = AsyncResult.bind f ar

[<AutoOpen>]
module AsyncResultExpressionBuilder =
  let asyncResult = new AsyncResultBuilder()

type ReaderAsyncResult<'TRead, 'T, 'TError> = Operation of ('TRead -> Async<Result<'T,'TError>>)

module ReaderAsyncResult =
  let map f (Operation action) =
    let mapped auth =
      action auth |> AsyncResult.map f
    Operation mapped

  let bind (f : 'a -> ReaderAsyncResult<'r,'b,'c>) (Operation action) =
    let f' auth =
      let f'' a =
        let (Operation action') = f a
        let res' = action' auth
        res'
      action auth |> AsyncResult.bind f''
    Operation f'

  let lift v =
    Operation(fun _ -> AsyncResult.lift v)

  let run (Operation action) auth =
    action auth

type ReaderAsyncResultBuilder<'TRead>() =
  member this.Return(v) = ReaderAsyncResult.lift v
  member this.ReturnFrom(v) = v
  member this.Bind(m, f) = ReaderAsyncResult.bind f m
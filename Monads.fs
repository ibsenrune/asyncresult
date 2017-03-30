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

type AsyncResultBuilder() =
  member this.Return(v) = AsyncResult.lift v
  member this.ReturnFrom(v) = v
  member this.Bind(ar, f) = AsyncResult.bind f ar

[<AutoOpen>]
module AsyncResultExpressionBuilder =
  let asyncResult = new AsyncResultBuilder()

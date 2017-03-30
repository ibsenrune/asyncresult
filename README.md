# asyncResult

An `Async<Result<'T, 'TError>>` computation expression for F# 4.1 that makes it easier 
to work with asynchronous operations that may fail.

The `AsyncResult` module includes

* A `map` function.
* A `bind` function.
* A `lift` function.
* A [`traverse`](https://fsharpforfunandprofit.com/posts/elevated-world-4/#traverse) function.
* An `asyncResult` computation expression.

## Usage

Assume that we have the following functions

```
> let lookupUser userId = /* Try to look up user in database */
val lookupUser : userId:int -> Async<Result<User,string>>

> let lookupOrders user = /* Try to look up the user's orders */
val lookupOrders : user:User -> Async<Result<Order list,string>>

```

We can combine them by first trying to look up the user and, if that succeeds, 
trying to look up the user's orders:

```
> let lookupOrdersByUserId userId =
  asyncResult {
    let! user = lookupUser userId
    let! order = lookupOrders user
    return order
  };;
val lookupOrdersByUserId : userId:int -> Async<Result<Order list,string>>
```

If we have a list of user identifiers, we could look up all users by using 
`List.map`:

```

> let users = [1;2;3] |> List.map lookupUser;;
val users : Async<Result<User,string>> list

```
However, this gives us a *list of asynchronous results*, which is not immediately usable. 
Instead, what we want is an *asynchronous result of a list* of users. This is 
where `AsyncResult.traverse` comes to the rescue:

```
> let users = [1;2;3] |> AsyncResult.traverse lookupUser;;
val users : Async<Result<User list,string>>

```
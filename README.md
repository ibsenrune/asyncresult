# asyncResult

The asyncResult project contains utilities to hide the complexities of working with
asynchronous operations that may fail. In particular, in contains

* an `AsyncResult` module for working with asynchronous operations that may or may not succeed. 
* a `ReaderAsyncResult` module for working with asynchronous operations that all require 
  some piece of information and may or may not fail.

## AsyncResult

When working with asynchronous operations that may fail, you often end up with types of the form `Async<Result<'T, 'TError>>`. For example, looking up a user in a database might result in a value of type `Async<Result<User, unit>>`, while calling an API over HTTP to purchase a product might result in a value of type `Async<Result<Product, PurchaseFailure>>`.

Both the `Result` and `Async` types are about control flow, so they are usually not the values you really care about - you care about the values they contain. Thus, you end up unwrapping them all the time. Even if you use the build in `async` workflow, which does make working with `Async` tolerable, there is still the `Result<'a,'b>` type to worry about. 

The `AsyncResult` module provides functions and computation expressions to make working with these values a breeze, enabling you to easily create, transform, and unwrap such values, and with the `asyncResult` computation expression, you can easily build and combine long workfldows that produce such values.

The `AsyncResult` module includes

* A `map` function.
* A `bind` function.
* A `lift` function.
* A [`traverse`](https://fsharpforfunandprofit.com/posts/elevated-world-4/#traverse) function.
* An `asyncResult` computation expression.

### Usage

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

## ReaderAsyncResult

The `ReaderAsyncResult` module contains utilities for working with asynchronous 
operations that all require some piece of information and may or may not fail.

We encode this in a type:

```
type ReaderAsyncResult<'TRead, 'T, 'TError> = Operation of ('TRead -> Async<Result<'T,'TError>>)
```

This represents operations that take some piece of data, `'TRead`, and produce an asynchronous result.
The `ReaderAsyncResult` module provides functions for working with such operations:

* A `map` function.
* A `bind` function.
* A `lift` function.
* The ability to create computation expressions for such operations.

Now, that is all fairly generic, so let's try to be a bit more specific.

Imagine that our application needs to call an external API over HTTP. This API 
requires the client (our application) to pass an authorization token along with 
each request. In our code, we represent this with a type

```
type AuthorizationToken = AuthorizationToken of string
```

Operations calling the external API then have type

```
AuthorizationToken -> Async<Result<'T,'TError>>
```

Now let's image we have a multitude of operations,
of which some may take more arguments than the `AuthorizationToken`:

```
getOrderFromBasket : unit -> AuthorizationToken -> Async<Result<BasketOrder, Error>>
placeOrder : BasketOrder -> AuthorizationToken -> Async<Result<Order, Error>>
shipOrder : Order -> AuthorizationToken -> Async<Result<unit, Error>>
cancelOrder : Order -> AuthorizationToken -> Async<Result<Order, Error>>
```

We can phrase these in terms of `ReaderAsyncResult<'TRead, 'T, 'TError>`:

```
getOrderFromBasket : unit -> ReaderAsyncResult<AuthorizationToken, BasketOrder, Error>
placeOrder : BasketOrder -> ReaderAsyncResult<AuthorizationToken, Order, Error>
shipOrder : Order -> ReaderAsyncResult<AuthorizationToken, unit, Error>
cancelOrder : Order -> ReaderAsyncResult<AuthorizationToken, Order, Error>
```

By using a `ReaderAsyncResultBuilder<'TRead>`, we can now easily combine such functions:

```
let withApi = new ReaderAsyncResultBuilder<AuthorizationToken>()

let purchase = withApi {
  let! basketOrder = getOrderFromBasket()
  let! order = placeOrder basketOrder
  do! shipOrder order
}
```

The result of this is

```
val purchase : ReaderAsyncResult<AuthorizationToken, unit, Error>
```

At this point, `purchase` is just an operation that is ready to run. No calls to the HTTP API have been made yet, because we have never provided an `AuthorizationToken`. That's what we will do next. 

The `ReaderAsyncResult` module contains a `run` function that we can use to execute the operation:

```
run : ReaderAsyncResult<'a, 'b, 'c> -> 'a -> Async<Result<'b,'c>>
```

Our `purchase` operation requires an `AuthorizationToken` to run, so that is what we will give it:

```
let authToken : AuthorizationToken = /* get auth token from somewhere */
let result = authToken |> run purchase |> Async.RunSynchronously
/* val result : Result<unit, Error> */
```


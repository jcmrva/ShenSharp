﻿namespace ShenPOF

open Microsoft.VisualStudio.TestTools.UnitTesting
open FParsec
open KlTokenizer
open KlParser
open KlEvaluator
open KlBuiltins

[<TestClass>]
type Tests() =

    let runInEnv env = tokenize >> parse Head >> eval env >> go
    let runit = runInEnv (baseEnv ()) >> go
    let getError = function
        | ValueResult (ErrorValue e) -> e
        | _ -> invalidArg "_" "not an Error"
    let getNumber = function
        | ValueResult (NumberValue n) -> n
        | _ -> invalidArg "_" "not a Number"
    let getString = function
        | ValueResult (StringValue s) -> s
        | _ -> invalidArg "_" "not a String"
    let getVector = function
        | ValueResult (VectorValue s) -> s
        | _ -> invalidArg "_" "not a Vector"
    let getUncaught = function
        | ErrorResult (Uncaught s) -> s
        | _ -> invalidArg "_" "not an Uncaught"
    let arrayEqual (xs : 'a[]) (ys : 'a[]) =
        xs.Length = ys.Length && Array.forall2 (=) xs ys
    let getFunc = function
        | ValueResult (FunctionValue f) -> f
        | _ -> invalidArg "_" "not a Function"
    let trueV = BoolValue true
    let falseV = BoolValue false
    let trueR = BoolValue true |> ValueResult
    let falseR = BoolValue false |> ValueResult
    let eApp1 f arg1 = AppExpr(Head, f, [arg1])
    let symApp1 sym arg1 = AppExpr (Head, SymbolExpr sym, [arg1])
    let symApp2 sym arg1 arg2 = AppExpr (Head, SymbolExpr sym, [arg1; arg2])
    let symApp3 sym arg1 arg2 arg3 = AppExpr (Head, SymbolExpr sym, [arg1; arg2; arg3])
    let intE = decimal >> NumberExpr
    let intV = decimal >> NumberValue
    let intR = intV >> ValueResult
    let funcV n f = FunctionValue <| new Function(1, f)

    [<TestMethod>]
    member this.TokenizerTest() =
        let tryit s = 
            match run pKlToken s with
                | Success(result, _, _)   -> ()
                | Failure(errorMsg, _, _) -> Assert.Fail(errorMsg)

        tryit "abc"
        tryit "123"
        tryit "()"
        tryit "(a)"
        tryit "(a b)"
        tryit "((a b) c (d e f) ((a) ()) c (d f))"
        tryit "(a     b)"

        // extra space tests
        // these aren't working
        // parse fails when there is extra space between
        // open paren and first child
        // or last child and close paren
        //tryit "(    )"
        //tryit "(   a)"
        //tryit "(a  )"
        //tryit "(   a   b  c   )"

    [<TestMethod>]
    member this.ParserTest() =
        let tryit syntax expr = 
            match run pKlToken syntax with
                | Success(result, _, _)   -> Assert.AreEqual(expr, parse Head result)
                | Failure(errorMsg, _, _) -> Assert.Fail(errorMsg)

        tryit "2" (intE 2)
        tryit "()" EmptyExpr
        tryit "(add 1 2)" (symApp2 "add" (intE 1) (intE 2))

    [<TestMethod>]
    member this.EvaluatorTest() =
        let tryit syntax expr = 
            match run pKlToken syntax with
                | Success(result, _, _)   -> Assert.AreEqual(ValueResult expr, result |> parse Head |> eval (emptyEnv ()))
                | Failure(errorMsg, _, _) -> Assert.Fail(errorMsg)

        // some basic expressions using special forms and literals only
        tryit "()" EmptyValue
        tryit "true" trueV
        tryit "2" (intV 2)
        tryit "(and true true)" trueV
        tryit "(and true false)" falseV
        tryit "(and false true)" falseV
        tryit "(and false false)" falseV
        tryit "(or true true)" trueV
        tryit "(or true false)" trueV
        tryit "(or false true)" trueV
        tryit "(or false false)" falseV

    [<TestMethod>]
    member this.DefunAndResolutionOfDefinedFunctions() =
        let (globals, locals) as env = emptyEnv ()
        globals.Add("not", funcV 1 (function | [BoolValue b] -> not b |> BoolValue |> ValueResult
                                             | _             -> invalidArg "args" "must be bool"))
        runInEnv env "(defun xor (l r) (or (and l (not r)) (and (not l) r)))" |> ignore
        Assert.AreEqual(trueR, runInEnv env "(xor true false)")

    [<TestMethod>]
    member this.SymbolResolution() =
        let (globals, locals) as env = emptyEnv ()
        globals.Add("symbol?", funcV 1 (function | [SymbolValue _] -> trueR
                                                 | _               -> falseR))
        Assert.AreEqual(trueR, runInEnv env "(symbol? run)")
        globals.Add("id", funcV 1 (function | [x] -> ValueResult x
                                            | _   -> invalidArg "args" "must be 1 arg"))
        Assert.AreEqual(trueR, runInEnv env "(symbol? (id run))")

    [<TestMethod>]
    member this.PartialApplicationForBuiltins() =
        let env = baseEnv ()
        Assert.AreEqual(intR 3, eval env (symApp2 "+" (intE 1) (intE 2)))
        Assert.AreEqual(intR 3, eval env (eApp1 (symApp1 "+" (intE 1)) (intE 2)))

    [<TestMethod>]
    member this.Builtins() =
        Assert.AreEqual(intR 3, runit "((+ 1) 2)")
        Assert.AreEqual(intR 2, runit "((- 4) 2)")
        Assert.AreEqual(falseR, runit "(cons? ())")
        Assert.AreEqual(falseR, runit "(cons? 0)")
        Assert.AreEqual(trueR, runit "(cons? (cons 0 0))")
    
    [<TestMethod>]
    member this.StringFunctions() =
        Assert.AreEqual("Hello, World!", runit "(cn \"Hello, \" \"World!\")" |> getString)
        Assert.AreEqual("Hello", runit "(cn (n->string (string->n \"Hello\")) (strtl \"Hello\"))" |> getString)
        Assert.AreEqual("1", runit "(str 1)" |> getString)
        
    [<TestMethod>]
    member this.Vectors() =
        Assert.IsTrue(arrayEqual Array.empty<KlValue> (runit "(absvector 0)" |> getVector))
        Assert.IsTrue(arrayEqual [|EmptyValue|] (runit "(absvector 1)" |> getVector))
        Assert.IsTrue(arrayEqual [|BoolValue true|] (runit "(address-> (absvector 1) 0 true)" |> getVector))

    [<TestMethod>]
    member this.EvalFunction() =
        Assert.AreEqual(intR 3, runit "(eval-kl (cons + (cons 1 (cons 2 ()))))")
        let inc = (runit >> getFunc) "(eval-kl (cons lambda (cons X (cons (cons + (cons 1 (cons X ()))) ()))))" // (lambda X (+ 1 X))
        Assert.AreEqual(intR 5, (inc .Apply [intV 4]) |> go)

    [<TestMethod>]
    member this.SanityChecks() =
        Assert.AreEqual(BoolToken true, BoolToken true)
        Assert.AreEqual(BoolExpr true, BoolExpr true)
        Assert.AreEqual(BoolValue true, BoolValue true) // this was failing when KlValue had a case containing a function type
        Assert.AreEqual(BoolValue true |> ValueResult, BoolValue true |> ValueResult) // this might start failing for the same reason

    [<TestMethod>]
    member this.TailRecursionOptimization() =
        let env = baseEnv ()
        runInEnv env "(defun fill (vec start stop val) (if (= stop start) (address-> vec start val) (fill (address-> vec start val) (+ 1 start) stop val)))" |> ignore
        let x = runInEnv env "(fill (absvector 20000) 0 19999 0)"
        ignore 0

    [<TestMethod>]
    member this.SimpleError() =
        let s = runit "(simple-error \"whoops\")" |> getUncaught
        Assert.AreEqual("whoops", s)

    [<TestMethod>]
    member this.PrintStuff() =
        let u = runit "(get-time unix)" |> getNumber
        printf "Unix time: %f" u
        printf "\r\n"
        let r = runit "(get-time run)" |> getNumber
        printf "Run time: %f" r
        printf "\r\n"
        let s = runit "(str (cons 1 (cons 2 (cons 3 ()))))" |> getString
        printf "Cons: %s" s
        printf "\r\n"
        let s = runit "(str (address-> (address-> (address-> (absvector 3) 0 1) 1 2) 2 3))" |> getString
        printf "Vector: %s" s
        printf "\r\n"
        let s = runit "(trap-error (simple-error \"whoops\") (lambda E E))" |> getError
        printf "Error: %s" s
        printf "\r\n"
        let s = runit "(str (trap-error (simple-error \"whoops\") (lambda Ex Ex)))" |> getString
        printf "Error-string: %s" s
        printf "\r\n"

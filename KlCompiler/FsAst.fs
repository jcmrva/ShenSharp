﻿namespace KlCompiler

open Microsoft.FSharp.Compiler.Ast
open Microsoft.FSharp.Compiler.Range

module FsAst =
    let defaultRange = mkFileIndexRange 50 (mkPos 100 100) (mkPos 200 200)

type FsId =

    static member Long(ids: string list) =
        let ident id = new Ident(id, FsAst.defaultRange)
        LongIdentWithDots.LongIdentWithDots(
            List.map ident ids,
            List.replicate ((ids.Length) - 1) FsAst.defaultRange)

type FsFile =

    static member Of(ns: string, modules: SynModuleOrNamespace list) =
        ParsedInput.ImplFile(
            ParsedImplFileInput(
                "filename",
                false,
                QualifiedNameOfFile(new Ident(ns, FsAst.defaultRange)),
                [],
                [],
                modules,
                false))

type FsConst =

    static member Bool(b: bool) = SynConst.Bool b

    static member Int32(x: int) = SynConst.Int32 x

    static member Decimal(d: decimal) = SynConst.Decimal d

    static member String(x: string) = SynConst.String(x, FsAst.defaultRange)

    static member Unit = SynConst.Unit
        
type FsType =

    static member ListOf(typeArg: SynType) =
        SynType.App(SynType.LongIdent(FsId.Long ["list"]), None, [typeArg], [FsAst.defaultRange], None, true, FsAst.defaultRange)
    
    static member Of(typeName: string) =
        SynType.LongIdent(
            LongIdentWithDots.LongIdentWithDots(
                [new Ident(typeName, FsAst.defaultRange)],
                []))

type FsModule =

    static member Of(name: string, members: SynModuleDecls) =
        SynModuleOrNamespace.SynModuleOrNamespace(
            [new Ident(name, FsAst.defaultRange)],
            true,
            members,
            PreXmlDocEmpty,
            [],
            None,
            FsAst.defaultRange)

    static member Open(parts: string list) =
        SynModuleDecl.Open(
            LongIdentWithDots.LongIdentWithDots(
                List.map (fun p -> new Ident(p, FsAst.defaultRange)) parts,
                List.replicate ((List.length parts) - 1) FsAst.defaultRange),
            FsAst.defaultRange)

    static member Let(bindings: SynBinding list) =
        SynModuleDecl.Let(false, bindings, FsAst.defaultRange)

    static member SingleLet(name: string, args: (string * SynType) list, body: SynExpr) =
        let eachArg (nm, _) = [SynArgInfo.SynArgInfo([], false, Some(new Ident(nm, FsAst.defaultRange)))]
        let argInfos = List.map eachArg args
        let valData =
            SynValData.SynValData(
                None,
                SynValInfo.SynValInfo(
                    argInfos,
                    SynArgInfo.SynArgInfo([], false, None)),
                None)
        let eachCtorArg (nm, typ) =
            SynPat.Paren(
                SynPat.Typed(
                    SynPat.Named(
                        SynPat.Wild(FsAst.defaultRange),
                        new Ident(nm, FsAst.defaultRange),
                        false,
                        None,
                        FsAst.defaultRange),
                    typ,
                    FsAst.defaultRange),
                FsAst.defaultRange)
        let ctorArgs = List.map eachCtorArg args
        let namePat =
            SynPat.LongIdent(
                LongIdentWithDots.LongIdentWithDots([new Ident(name, FsAst.defaultRange)], []),
                None,
                None,
                SynConstructorArgs.Pats(ctorArgs),
                None,
                FsAst.defaultRange)
        let returnType = FsType.Of("Value")
        let returnInfo = SynBindingReturnInfo.SynBindingReturnInfo(returnType, FsAst.defaultRange, [])
        let binding =
            SynBinding.Binding(
                None,
                SynBindingKind.NormalBinding,
                false,
                false,
                [],
                PreXmlDoc.Empty,
                valData,
                namePat,
                Some returnInfo,
                body,
                FsAst.defaultRange,
                SequencePointInfoForBinding.NoSequencePointAtLetBinding)
        SynModuleDecl.Let(false, [binding], FsAst.defaultRange)

    static member LetRec(bindings: SynBinding list) =
        SynModuleDecl.Let(true, bindings, FsAst.defaultRange)

type FsExpr =

    static member Bool(b: bool) =
        SynExpr.Const(SynConst.Bool b, FsAst.defaultRange)
        
    static member Int32(x: int) =
        SynExpr.Const(SynConst.Int32 x, FsAst.defaultRange)

    static member Decimal(d: decimal) =
        SynExpr.Const(SynConst.Decimal d, FsAst.defaultRange)

    static member String(x: string) =
        SynExpr.Const(SynConst.String(x, FsAst.defaultRange), FsAst.defaultRange)

    static member Unit = SynExpr.Const(SynConst.Unit, FsAst.defaultRange)
        
    static member Paren(expr: SynExpr) =
        SynExpr.Paren(expr, FsAst.defaultRange, None, FsAst.defaultRange)

    static member Id(id: string) =
        SynExpr.Ident(new Ident(id, FsAst.defaultRange))

    static member LongId(ids: string list) =
        let ident id = new Ident(id, FsAst.defaultRange)
        SynExpr.LongIdent(
            false,
            LongIdentWithDots.LongIdentWithDots(
                List.map ident ids,
                List.replicate ((ids.Length) - 1) FsAst.defaultRange),
            None,
            FsAst.defaultRange)

    static member ConsSequential(expr0: SynExpr, expr1: SynExpr) =
        SynExpr.Sequential(SequencePointsAtSeq, true, expr0, expr1, FsAst.defaultRange)

    static member Let(bindings: SynBinding list, body: SynExpr) =
        SynExpr.LetOrUse(false, false, bindings, body, FsAst.defaultRange)
        
    static member LetRec(bindings: SynBinding list, body: SynExpr) =
        SynExpr.LetOrUse(true, false, bindings, body, FsAst.defaultRange)

    static member Const(constant: SynConst) =
        SynExpr.Const(constant, FsAst.defaultRange)

    static member Tuple(exprs: SynExpr list) =
        FsExpr.Paren(SynExpr.Tuple(exprs, List.replicate exprs.Length FsAst.defaultRange, FsAst.defaultRange))

    static member List(exprs: SynExpr list) =
        SynExpr.ArrayOrList(false, exprs, FsAst.defaultRange)

    static member Lambda(isInnerLambda: bool, args: (string * SynType) list, body: SynExpr) =
        let buildArg (nm, typ) =
            SynSimplePat.Typed(
                SynSimplePat.Id(
                    new Ident(nm, FsAst.defaultRange),
                    None,
                    false,
                    false,
                    false,
                    FsAst.defaultRange),
                typ,
                FsAst.defaultRange)
        SynExpr.Paren(
            SynExpr.Lambda(
                false,
                isInnerLambda,
                SynSimplePats.SimplePats(
                    List.map buildArg args,
                    FsAst.defaultRange),
                body,
                FsAst.defaultRange),
            FsAst.defaultRange,
            None,
            FsAst.defaultRange)

    static member If(condition: SynExpr, ifTrue: SynExpr, ifFalse: SynExpr) =
        SynExpr.IfThenElse(
            condition,
            ifTrue,
            Some ifFalse,
            SequencePointInfoForBinding.NoSequencePointAtInvisibleBinding,
            false,
            FsAst.defaultRange,
            FsAst.defaultRange)

    static member App(f0: SynExpr, args0: SynExpr list) =
        let rec buildApply f args =
            match args with
            | single :: [] ->
                SynExpr.App(
                    ExprAtomicFlag.NonAtomic,
                    false,
                    f,
                    single,
                    FsAst.defaultRange)
            | first :: rest ->
                buildApply (SynExpr.App(ExprAtomicFlag.NonAtomic, false, f, first, FsAst.defaultRange)) rest
            | _ -> failwith "must have at least one argument"
        FsExpr.Paren(buildApply f0 args0)

    static member Constructor(typeName: string, arg: SynExpr) =
        FsExpr.Paren(SynExpr.New(false, FsType.Of(typeName), arg, FsAst.defaultRange))

    static member Infix(lhs: SynExpr, op: SynExpr, rhs: SynExpr) =
        FsExpr.App(op, [lhs; rhs])

    static member Match(key: SynExpr, clauses: SynMatchClause list) =
        SynExpr.Match(
            SequencePointInfoForBinding.SequencePointAtBinding FsAst.defaultRange,
            key,
            clauses,
            false,
            FsAst.defaultRange)

    static member Try(body: SynExpr, handlers: SynMatchClause list) =
        SynExpr.TryWith(
            body,
            FsAst.defaultRange,
            handlers,
            FsAst.defaultRange,
            FsAst.defaultRange,
            SequencePointInfoForTry.SequencePointAtTry FsAst.defaultRange,
            SequencePointInfoForWith.SequencePointAtWith FsAst.defaultRange)

type FsPat =

    static member Const(c: SynConst) =
        SynPat.Const(c, FsAst.defaultRange)

    static member Name(ids: string list, pats: SynPat list) =
        SynPat.Paren(
            SynPat.LongIdent(
                FsId.Long ids,
                None,
                None,
                SynConstructorArgs.Pats(pats),
                None,
                FsAst.defaultRange),
            FsAst.defaultRange)

    static member Name(ids: string list) = FsPat.Name(ids, [])

    static member Name(id: string, pats: SynPat list) = FsPat.Name([id], pats)

    static member Name(id: string, pat: SynPat) = FsPat.Name([id], [pat])

    static member Name(id: string) = FsPat.Name([id], [])

    static member SimpleName(id: string) = FsPat.Name(id)

    static member List(items: SynPat list) =
        SynPat.ArrayOrList(false, items, FsAst.defaultRange)

    static member Wild = SynPat.Wild(FsAst.defaultRange)
         

type FsMatchClause =

    static member Of(pat: SynPat, body: SynExpr) =
        SynMatchClause.Clause(
            pat,
            None,
            body,
            FsAst.defaultRange,
            SequencePointInfoForTarget.SequencePointAtTarget)

type FsBinding =

    static member Of(symbol: string, body: SynExpr) =
        SynBinding.Binding(
            None,
            SynBindingKind.NormalBinding,
            false,
            false,
            [],
            PreXmlDoc.Empty,
            SynValData.SynValData(
                None,
                SynValInfo.SynValInfo([], SynArgInfo.SynArgInfo([], false, None)),
                None),
            SynPat.Named(
                SynPat.Wild FsAst.defaultRange,
                new Ident(symbol, FsAst.defaultRange),
                false,
                None,
                FsAst.defaultRange),
            None,
            body,
            FsAst.defaultRange,
            SequencePointInfoForBinding.NoSequencePointAtLetBinding)

type FsFail =

    static member With(s: string) =
        FsExpr.App(FsExpr.Id("failwith"), [FsExpr.String(s)])
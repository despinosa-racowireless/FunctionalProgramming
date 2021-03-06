﻿using System;
using System.Collections.Generic;
using System.Linq;
using FunctionalProgramming.Basics;

namespace FunctionalProgramming.Monad.Transformer
{
    public sealed class IoMaybeEnumerable<T>
    {
        private readonly Io<IMaybe<IEnumerable<T>>> _self;

        public IoMaybeEnumerable(Io<IMaybe<IEnumerable<T>>> io)
        {
            _self = io;
        }

        public IoMaybeEnumerable(T t)
            : this(Io.Apply(() => t.LiftEnumerable().ToMaybe()))
        {

        }

        public IoMaybeEnumerable<TResult> FMap<TResult>(Func<T, TResult> f)
        {
            return new IoMaybeEnumerable<TResult>(_self.Select(m => m.Select(enumerable => enumerable.Select(f))));
        }

        public Io<IMaybe<IEnumerable<T>>> Out()
        {
            return _self;
        }

        public IoMaybeEnumerable<TResult> Bind<TResult>(Func<T, IoMaybeEnumerable<TResult>> f)
        {
            return new IoMaybeEnumerable<TResult>(_self.SelectMany(m => m.Match(
                just: enumerable => enumerable.Select(arg => f(arg).Out()).Sequence().Select(maybes => maybes.Sequence().Select(e => e.SelectMany(BasicFunctions.Identity))),
                nothing: () => IoMaybeEnumerable.NothingIo<TResult>().Out())));
        }
    }

    public static class IoMaybeEnumerable
    {
        public static IoMaybeEnumerable<T> ToIoMaybeEnumerable<T>(this Io<IMaybe<IEnumerable<T>>> io)
        {
            return new IoMaybeEnumerable<T>(io);
        }

        public static IoMaybeEnumerable<T> ToIoMaybeEnumerable<T>(this T t)
        {
            return new IoMaybeEnumerable<T>(t);
        }

        public static IoMaybeEnumerable<T> ToIoMaybeEnumerable<T>(this Io<IEnumerable<T>> io)
        {
            return new IoMaybeEnumerable<T>(io.Select(t => t.ToMaybe()));
        }

        public static IoMaybeEnumerable<T> ToIoMaybeEnumerable<T>(this IMaybe<IEnumerable<T>> m)
        {
            return new IoMaybeEnumerable<T>(Io.Apply(() => m));
        }

        public static IoMaybeEnumerable<T> NothingIo<T>()
        {
            return Maybe.Nothing<IEnumerable<T>>().ToIoMaybeEnumerable();
        }

        public static IoMaybeEnumerable<T> Where<T>(this IoMaybeEnumerable<T> ioT, Func<T, bool> predicate)
        {
            return new IoMaybeEnumerable<T>(ioT.Out().Select(m => m.Select(enumerable => enumerable.Where(predicate))));
        }

        public static IoMaybeEnumerable<TResult> Select<TInitial, TResult>(this IoMaybeEnumerable<TInitial> ioT, Func<TInitial, TResult> f)
        {
            return ioT.FMap(f);
        }

        public static IoMaybeEnumerable<TResult> SelectMany<TInitial, TResult>(this IoMaybeEnumerable<TInitial> ioT, Func<TInitial, IoMaybeEnumerable<TResult>> f)
        {
            return ioT.Bind(f);
        }

        public static IoMaybeEnumerable<TSelect> SelectMany<TInitial, TResult, TSelect>(this IoMaybeEnumerable<TInitial> ioT, Func<TInitial, IoMaybeEnumerable<TResult>> f, Func<TInitial, TResult, TSelect> selector)
        {
            return ioT.SelectMany(a => f(a).SelectMany(b => selector(a, b).ToIoMaybeEnumerable()));
        }
    }
}

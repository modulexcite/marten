﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Baseline;
using Marten.Util;

namespace Marten.Events.Projections
{
    public class Aggregator<T> : IAggregator where T : class, new()
    {
        public static readonly string ApplyMethod = "Apply";

        private readonly IDictionary<Type, object> _aggregations = new Dictionary<Type, object>();


        public Aggregator() 
        {
            typeof (T).GetMethods()
                .Where(x => x.Name == ApplyMethod && x.GetParameters().Length == 1)
                .Each(method =>
                {
                    var eventType = method.GetParameters().Single<ParameterInfo>().ParameterType;
                    var step = typeof (AggregationStep<,>)
                        .CloseAndBuildAs<object>(method, typeof (T), eventType);

                    _aggregations.Add(eventType, step);
                });

            Alias = typeof (T).Name.ToTableAlias();
        }

        public Type AggregateType => typeof (T);

        public string Alias { get; }

        public T Build(IEnumerable<IEvent> events)
        {
            var state = new T();

            events.Each(x => x.Apply(state, this));

            return state;
        }

        public Aggregator<T> Add<TEvent>(IAggregation<T, TEvent> aggregation)
        {
            if (_aggregations.ContainsKey(typeof (TEvent)))
            {
                _aggregations[typeof (TEvent)] = aggregation;
            }
            else
            {
                _aggregations.Add(typeof(TEvent), aggregation);
            }

            return this;
        }

        public Aggregator<T> Add<TEvent>(Action<T, TEvent> application)
        {
            return Add(new AggregationStep<T, TEvent>(application));
        }

        public IAggregation<T, TEvent> AggregatorFor<TEvent>()
        {
            return _aggregations.ContainsKey(typeof (TEvent))
                ? _aggregations[typeof (TEvent)].As<IAggregation<T, TEvent>>()
                : null;
        }


        public bool AppliesTo(EventStream stream)
        {
            return stream.Events.Any(x => _aggregations.ContainsKey(x.Data.GetType()));
        }
    }
}
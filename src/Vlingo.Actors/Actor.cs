﻿// Copyright (c) 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using Vlingo.Actors.TestKit;

namespace Vlingo.Actors
{
    public abstract class Actor : IStartable, IStoppable, ITestStateView
    {
        internal readonly BasicCompletes<object> completes;
        internal LifeCycle LifeCycle { get; }

        public virtual Address Address => LifeCycle.Address;

        public virtual IDeadLetters DeadLetters => LifeCycle.Environment.Stage.World.DeadLetters;

        public virtual Scheduler Scheduler => LifeCycle.Environment.Stage.Scheduler;

        public virtual void Start()
        {
        }

        public virtual bool IsStopped => LifeCycle.IsStopped;

        public virtual void Stop()
        {
            if (!IsStopped)
            {
                if (LifeCycle.Address.Id != World.DeadlettersId)
                {
                    LifeCycle.Stop(this);
                }
            }
        }

        public virtual TestState ViewTestState() => new TestState();

        public override bool Equals(object other)
        {
            if (other == null || other.GetType() != GetType())
            {
                return false;
            }

            return Address.Equals(((Actor) other).LifeCycle.Address);
        }

        public override int GetHashCode() => LifeCycle.GetHashCode();

        public override string ToString() => $"Actor[type={GetType().Name} address={Address}]";

        protected Actor()
        {
            var maybeEnvironment = ActorFactory.ThreadLocalEnvironment.Value;
            LifeCycle = new LifeCycle(maybeEnvironment ?? new TestEnvironment());
            ActorFactory.ThreadLocalEnvironment.Value = null;
            completes = new BasicCompletes<object>();
        }

        protected T ChildActorFor<T>(Definition definition)
        {
            if (definition.Supervisor != null)
            {
                return LifeCycle.Environment.Stage.ActorFor<T>(definition, this, definition.Supervisor,
                    Logger);
            }
            else
            {
                if (this is ISupervisor)
                {
                    return LifeCycle.Environment.Stage.ActorFor<T>(
                        definition, 
                        this,
                        LifeCycle.LookUpProxy<ISupervisor>(),
                        Logger);
                }
                else
                {
                    return LifeCycle.Environment.Stage.ActorFor<T>(definition, this, null, Logger);
                }
            }
        }

        internal ICompletes<T> Completes<T>()
        {
            if(completes == null)
            {
                throw new InvalidOperationException("Completes is not available for this protocol behavior");
            }

            return (ICompletes<T>)completes;
        }

        protected Definition Definition => LifeCycle.Definition;

        internal ILogger Logger => LifeCycle.Environment.Logger;

        protected Actor Parent
        {
            get
            {
                if (LifeCycle.Environment.IsSecured)
                {
                    throw new InvalidOperationException("A secured actor cannot provide its parent.");
                }

                return LifeCycle.Environment.Parent;
            }
        }

        protected void Secure()
        {
            LifeCycle.Secure();
        }

        internal T SelfAs<T>()
        {
            return LifeCycle.Environment.Stage.ActorProxyFor<T>(this, LifeCycle.Environment.Mailbox);
        }

        protected IOutcomeInterest<TOutcome> SelfAsOutcomeInterest<TOutcome, TRef>(TRef reference)
        {
            var outcomeAware = LifeCycle.Environment.Stage.ActorProxyFor<IOutcomeAware<TOutcome, TRef>>(
                this,
                LifeCycle.Environment.Mailbox);

            return new OutcomeInterestActorProxy<TOutcome, TRef>(outcomeAware, reference);
        }

        internal Stage Stage
        {
            get
            {
                if (LifeCycle.Environment.IsSecured)
                {
                    throw new InvalidOperationException("A secured actor cannot provide its stage.");
                }

                return LifeCycle.Environment.Stage;
            }
        }

        protected Stage StageNamed(string name)
        {
            return LifeCycle.Environment.Stage.World.StageNamed(name);
        }


        internal bool IsDispersing =>
            LifeCycle.Environment.Stowage.IsDispersing;


        protected void DisperseStowedMessages()
        {
            LifeCycle.Environment.Stowage.DispersingMode();
        }

        internal bool IsStowing =>
            LifeCycle.Environment.Stowage.IsStowing;


        protected void StowMessages()
        {
            LifeCycle.Environment.Stowage.StowingMode();
        }

        //=======================================
        // life cycle overrides
        //=======================================

        internal virtual void BeforeStart()
        {
            // override
        }

        internal virtual void AfterStop()
        {
            // override
        }

        protected virtual void BeforeRestart(Exception reason)
        {
            // override
            LifeCycle.AfterStop(this);
        }

        internal virtual void AfterRestart(Exception reason)
        {
            // override
            LifeCycle.BeforeStart(this);
        }

        internal virtual void BeforeResume(Exception reason)
        {
            // override
        }
    }
}
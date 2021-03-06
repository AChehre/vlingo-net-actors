﻿// Copyright (c) 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;

namespace Vlingo.Actors
{
    public class LocalMessage<T> : IMessage
    {
        private readonly ICompletes completes;

        public LocalMessage(Actor actor, Action<T> consumer, ICompletes completes, string representation)
        {
            Actor = actor;
            Consumer = consumer;
            Representation = representation;
            this.completes = completes;
        }

        public LocalMessage(Actor actor, Action<T> consumer, string representation)
            : this(actor, consumer, null, representation)
        {
        }

        public LocalMessage(LocalMessage<T> message)
            : this(message.Actor, message.Consumer, null, message.Representation)
        {
        }

        public Actor Actor { get; }

        private Action<T> Consumer { get; }

        public virtual void Deliver()
        {
            if (Actor.LifeCycle.IsResuming)
            {
                if (IsStowed)
                {
                    InternalDeliver(this);
                }
                else
                {
                    InternalDeliver(Actor.LifeCycle.Environment.Suspended.SwapWith<T>(this));
                }
                Actor.LifeCycle.NextResuming();
            }
            else if (Actor.IsDispersing)
            {
                InternalDeliver(Actor.LifeCycle.Environment.Stowage.SwapWith<T>(this));
            }
            else
            {
                InternalDeliver(this);
            }
        }

        public virtual string Representation { get; }

        public virtual bool IsStowed => false;

        public override string ToString() => $"LocalMessage[{Representation}]";

        private void DeadLetter()
        {
            var deadLetter = new DeadLetter(Actor, Representation);
            var deadLetters = Actor.DeadLetters;
            if(deadLetters != null)
            {
                deadLetters.FailedDelivery(deadLetter);
            }
            else
            {
                Actor.Logger.Log($"vlingo-dotnet/actors: MISSING DEAD LETTERS FOR: {deadLetter}");
            }
        }

        private void InternalDeliver(IMessage message)
        {
            if (Actor.IsStopped)
            {
                DeadLetter();
            }
            else if (Actor.LifeCycle.IsSuspended)
            {
                Actor.LifeCycle.Environment.Suspended.Stow<T>(message);
            }
            else if (Actor.IsStowing)
            {
                Actor.LifeCycle.Environment.Stowage.Stow<T>(message);
            }
            else
            {
                try
                {
                    Actor.completes = completes;
                    Consumer.Invoke((T)(object)Actor);
                    if (Actor.completes != null && Actor.completes.HasOutcome)
                    {
                        var outcome = Actor.completes.Outcome;
                        Actor.LifeCycle.Environment.Stage.World.CompletesFor(completes).With(outcome);
                    }
                }
                catch(Exception ex)
                {
                    Actor.Logger.Log($"Message#Deliver(): Exception: {ex.Message} for Actor: {Actor} sending: {Representation}", ex);
                    Actor.Stage.HandleFailureOf<T>(new StageSupervisedActor<T>(Actor, ex));
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System.Linq;

namespace PathIntegrationTask
{
    public interface IObserver
    {
        void UpdateObserver();
    }

    public class Observer : MonoBehaviour, IObserver
    {
        public string ObserverName { get; protected set; }

        public virtual void UpdateObserver()
        {
           
        }

        #region Unity Methods
        protected virtual void Start()
        {

        }

        protected virtual void Update()
        {

        }
        #endregion
    }

    public interface ISubject
    {
        void Subscribe(Observer observer);
        void Notify();
    }

    public class Subject : MonoBehaviour, ISubject
    {
        private List<Observer> observers = new List<Observer>();

        public void Subscribe(Observer observer)
        {
            observers.Add(observer);
        }

        public void Notify()
        {
            observers.ForEach(x => x.UpdateObserver());
        }

        #region Unity Methods
        protected virtual void Start()
        {

        }

        protected virtual void Update()
        {

        }
        #endregion

    }


}

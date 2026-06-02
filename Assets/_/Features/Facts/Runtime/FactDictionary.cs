using System;
using System.Collections.Generic;
using TheFundation.Runtime.Data;
using UnityEngine;

namespace TheFundation.Runtime
{
    public class FactDictionary
    {
        
        #region Publics
        
        public enum FactPersistence {
            Normal,
            Persistent
        }

        public IReadOnlyDictionary<string, IFact> m_allFacts => _facts;

        public event Action<string, object> FactChanged; 
        
        #endregion
        
        
        #region Utils

        public bool TryGetFact<T>(string key, out T value)
        {
            if (_facts.TryGetValue(key, out var fact))
            {
                if (fact.GetObjectValue() is T casted)
                {
                    value = casted;
                    return true;
                }
            }

            value = default;
            return false;
        }
        
        public void RemoveFact(string key)
        {
            if (_facts.Remove(key))
                {
                FactChanged?.Invoke(key, null); // passer null pour signaler suppression
                }
        }

        public T GetFact<T>(string key)
        {
            if (!_facts.TryGetValue(key, out var fact)) throw new KeyNotFoundException("No Fact");
            if (fact is not Facts<T> typedFact) throw new InvalidCastException("Fact is not of type T");
            
            return typedFact.Value;
        }

        public void SetFact<T>(string key, T value, FactPersistence persistence = FactPersistence.Normal, bool save = true)
        {
            
            bool isPersistent = persistence == FactPersistence.Persistent;
            
            if (_facts.TryGetValue(key, out IFact existingFact))
            {
                existingFact.SetObjectValue(value);
                existingFact.IsPersistent = isPersistent;
                existingFact.IsSaved = save;
                
            }
            else
            {
                _facts[key] = new Facts<T>(value, isPersistent, save);
            }
            
            //notifier les abonnés
            FactChanged?.Invoke(key, value);
        }
        
        public void Declare<T>(string key, FactPersistence persistence)
        {
            bool isPersistent = persistence == FactPersistence.Persistent;

            if (_facts.TryGetValue(key, out IFact existing))
            {
                // ⚠️ IMPORTANT :
                // On NE TOUCHE PAS à la valeur
                // On garantit juste les flags
                existing.IsPersistent = isPersistent;
                existing.IsSaved = true;
                return;
            }

            // Fact jamais existant → création neutre
            _facts[key] = new Facts<T>(
                default,
                isPersistent
            );
        }

        public void ClearAll()
        {
            _facts.Clear();
            FactChanged?.Invoke(string.Empty, null);
        }

        
        #endregion
        
        
        #region Private And Protected
        
        private Dictionary<string, IFact> _facts = new();
        
        #endregion

    }
}

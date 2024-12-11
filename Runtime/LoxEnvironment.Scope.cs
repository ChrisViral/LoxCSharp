using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Lox.Exceptions.Runtime;
using Lox.Scanner;

namespace Lox.Runtime;

public partial class LoxEnvironment
{
    /// <summary>
    /// Scope data table
    /// </summary>
    private class Scope : IDictionary<string, LoxValue>
    {
        #region Constants
        /// <summary>
        /// Default scope capacity
        /// </summary>
        private const int DEFAULT_SCOPE_CAPACITY = 4;
        #endregion

        #region Fields
        /// <summary>
        /// Environment backing dictionary
        /// </summary>
        protected readonly Dictionary<string, LoxValue> values = new(DEFAULT_SCOPE_CAPACITY, StringComparer.Ordinal);
        #endregion

        #region Properties
        /// <summary>
        /// The number of variables held in the Environment
        /// </summary>
        public int Count => this.values.Count;

        /// <summary>
        /// Gets or sets a variable for the given identifier.
        /// </summary>
        /// <param name="identifier">Identifier token to get/set the variable for</param>
        /// <exception cref="LoxRuntimeException">If the variable being get/set does not exist</exception>
        public virtual LoxValue this[in Token identifier]
        {
            get => this.values.TryGetValue(identifier.Lexeme, out LoxValue value)
                       ? value
                       : throw new LoxRuntimeException($"Undefined variable '{identifier.Lexeme}'.", identifier);
            set => this.values[identifier.Lexeme] = this.values.ContainsKey(identifier.Lexeme)
                                                        ? value
                                                        : throw new LoxRuntimeException($"Undefined variable '{identifier.Lexeme}'.", identifier);
        }

        /// <summary>
        /// Collection of all the variable identifiers in the environment
        /// </summary>
        public Dictionary<string, LoxValue>.KeyCollection Keys => this.values.Keys;

        /// <summary>
        /// Collection of all the variable values in the environment
        /// </summary>
        public Dictionary<string, LoxValue>.ValueCollection Values => this.values.Values;
        #endregion

        #region Methods
        /// <summary>
        /// Creates a new variable for the given identifier with the specified value
        /// </summary>
        /// <param name="identifier">Variable identifier token</param>
        /// <param name="value">Variable value</param>
        public virtual void DefineVariable(in Token identifier, in LoxValue value) => this.values[identifier.Lexeme] = value;

        /// <summary>
        /// Tries and set the variable for the given identifier
        /// </summary>
        /// <param name="identifier">Variable identifier token</param>
        /// <param name="value">Variable value</param>
        /// <returns><see langword="true"/> if the variable was found and set, otherwise <see langword="false"/></returns>
        public virtual bool TrySetVariable(in Token identifier, in LoxValue value)
        {
            if (!this.values.ContainsKey(identifier.Lexeme)) return false;

            this.values[identifier.Lexeme] = value;
            return true;

        }

        /// <summary>
        /// Tries to get the value of a variable for the given identifier
        /// </summary>
        /// <param name="identifier">Variable identifier token</param>
        /// <param name="value">Variable value output parameter</param>
        /// <returns><see langword="true"/> if the variable was found, otherwise <see langword="false"/></returns>
        public virtual bool TryGetVariable(in Token identifier, out LoxValue value) => this.values.TryGetValue(identifier.Lexeme, out value);

        /// <summary>
        /// Checks if a variable exists for the given identifier
        /// </summary>
        /// <param name="identifier">Variable identifier token</param>
        /// <returns><see langword="true"/> if the variable exists, otherwise <see langword="false"/></returns>
        public virtual bool IsVariableDefined(in Token identifier) => this.values.ContainsKey(identifier.Lexeme);

        /// <summary>
        /// Removes the variable for the given identifier
        /// </summary>
        /// <param name="identifier">Variable identifier token</param>
        /// <returns><see langword="true"/> if the variable was successfully removed, otherwise <see langword="false"/></returns>
        public virtual bool DeleteVariable(in Token identifier) => this.values.Remove(identifier.Lexeme);

        /// <summary>
        /// Clears all the variables held in the environment
        /// </summary>
        public virtual void Clear()
        {
            if (this == GlobalScope) throw new LoxRuntimeException("Cannot clear global scope.");
            this.values.Clear();
        }

        /// <inheritdoc cref="IDictionary{TKey,TValue}.GetEnumerator" />
        public Dictionary<string, LoxValue>.Enumerator GetEnumerator() => this.values.GetEnumerator();
        #endregion

        #region Implicit interface implementations
        /// <inheritdoc />
        bool ICollection<KeyValuePair<string, LoxValue>>.IsReadOnly => false;

        /// <inheritdoc />
        ICollection<string> IDictionary<string, LoxValue>.Keys => this.values.Keys;

        /// <inheritdoc />
        ICollection<LoxValue> IDictionary<string, LoxValue>.Values => this.values.Values;

        /// <inheritdoc />
        LoxValue IDictionary<string, LoxValue>.this[string key]
        {
            get => this.values[key];
            set => this.values[key] = value;
        }

        /// <inheritdoc />
        void IDictionary<string, LoxValue>.Add(string key, LoxValue value) => this.values.Add(key, value);

        /// <inheritdoc />
        bool IDictionary<string, LoxValue>.ContainsKey(string key) => this.values.ContainsKey(key);

        /// <inheritdoc />
        bool IDictionary<string, LoxValue>.TryGetValue(string key, out LoxValue value) => this.values.TryGetValue(key, out value);

        /// <inheritdoc />
        bool IDictionary<string, LoxValue>.Remove(string key) => this.values.Remove(key);

        /// <inheritdoc />
        void ICollection<KeyValuePair<string, LoxValue>>.Add(KeyValuePair<string, LoxValue> item) => ((ICollection<KeyValuePair<string, LoxValue>>)this.values).Add(item);

        /// <inheritdoc />
        bool ICollection<KeyValuePair<string, LoxValue>>.Contains(KeyValuePair<string, LoxValue> item) => this.values.Contains(item);

        /// <inheritdoc />
        void ICollection<KeyValuePair<string, LoxValue>>.CopyTo(KeyValuePair<string, LoxValue>[] array, int arrayIndex) => ((ICollection<KeyValuePair<string, LoxValue>>)this.values).CopyTo(array, arrayIndex);

        /// <inheritdoc />
        bool ICollection<KeyValuePair<string, LoxValue>>.Remove(KeyValuePair<string, LoxValue> item) => ((ICollection<KeyValuePair<string, LoxValue>>)this.values).Remove(item);

        /// <inheritdoc />
        IEnumerator<KeyValuePair<string, LoxValue>> IEnumerable<KeyValuePair<string, LoxValue>>.GetEnumerator() => this.values.GetEnumerator();

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => this.values.GetEnumerator();
        #endregion
    }

    /// <summary>
    /// Global scope data table
    /// </summary>
    private sealed class Global : Scope
    {
        #region Fields
        /// <summary>
        /// Native variable identifier set
        /// </summary>
        private readonly HashSet<string> natives = new(10, StringComparer.Ordinal);
        #endregion

        #region Properties
        /// <inheritdoc/>
        /// <exception cref="LoxRuntimeException">If the variable being get/set does not exist</exception>
        public override LoxValue this[in Token identifier]
        {
            get => base[identifier];
            set
            {
                if (this.natives.Contains(identifier.Lexeme)) throw new LoxRuntimeException($"Trying to redefine Lox native '{identifier.Lexeme}'.", identifier);
                base[identifier] = value;
            }
        }
        #endregion

        #region Methods
        /// <inheritdoc/>
        /// <exception cref="LoxRuntimeException">Trying to redefine a Lox native</exception>
        public override void DefineVariable(in Token identifier, in LoxValue value)
        {
            if (this.natives.Contains(identifier.Lexeme)) throw new LoxRuntimeException($"Trying to redefine Lox native '{identifier.Lexeme}'.", identifier);

            base.DefineVariable(identifier, value);
        }

        /// <summary>
        /// Creates a new native variable for the given identifier with the specified value
        /// </summary>
        /// <param name="identifier">Variable identifier token</param>
        /// <param name="value">Variable value</param>
        /// <exception cref="LoxRuntimeException">Trying to define an already existing native</exception>
        public void DefineNative(in Token identifier, in LoxValue value)
        {
            if (!this.natives.Add(identifier.Lexeme)) throw new LoxRuntimeException($"Already defined native '{identifier.Lexeme}'.", identifier);
            this.values[identifier.Lexeme] = value;
        }

        /// <inheritdoc/>
        /// <exception cref="LoxRuntimeException">Trying to set a Lox native</exception>
        public override bool TrySetVariable(in Token identifier, in LoxValue value)
        {
            if (this.natives.Contains(identifier.Lexeme)) throw new LoxRuntimeException($"Trying to set Lox native '{identifier.Lexeme}'.", identifier);

            return base.TrySetVariable(identifier, value);
        }

        /// <inheritdoc/>
        /// <exception cref="LoxRuntimeException">Trying to delete a Lox native</exception>
        public override bool DeleteVariable(in Token identifier)
        {
            if (this.natives.Contains(identifier.Lexeme)) throw new LoxRuntimeException($"Trying to delete Lox native '{identifier.Lexeme}'.", identifier);

            return base.DeleteVariable(identifier);
        }

        /// <summary>
        /// Global scopes cannot be clear, always throws a <see cref="LoxRuntimeException"/>
        /// </summary>
        /// <exception cref="LoxRuntimeException">Cannot clear global scope</exception>
        [DoesNotReturn]
        public override void Clear() => throw new LoxRuntimeException("Cannot clear global scope.");
        #endregion
    }
}

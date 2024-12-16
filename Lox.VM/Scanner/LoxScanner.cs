using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Lox.Common;

namespace Lox.VM.Scanner;

/// <summary>
/// Lox scanner
/// </summary>
public sealed partial class LoxScanner : ILoxScanner<Token>, IEnumerable<Token>, IDisposable
{
    #region Fields
    private GCHandle sourceHandle;
    private unsafe char* tokenStart;
    private unsafe char* currentChar;
    private int currentLine;
    private bool returnedEof;
    #endregion

    #region Properties
    private string sourceCode;
    /// <summary>
    /// Scanner's current source code
    /// </summary>
    /// <exception cref="ObjectDisposedException">If this scanner has been disposed</exception>
    /// <exception cref="InvalidOperationException">If the scanner is already scanning a source string</exception>
    /// <exception cref="ArgumentNullException">If <paramref name="value"/> is <see langword="null"/></exception>
    public string SourceCode
    {
        get => this.sourceCode;
        set
        {
            ObjectDisposedException.ThrowIf(this.IsDisposed, this);
            ThrowIfScanningState(this.IsScanning);
            this.sourceCode = value ?? throw new ArgumentNullException(nameof(value), "Source code cannot be null");
        }
    }

    /// <summary>
    /// If the scanner is currently mid-scan
    /// </summary>
    public bool IsScanning => this.sourceHandle.IsAllocated;

    /// <summary>
    /// If this scanner has been disposed
    /// </summary>
    public bool IsDisposed { get; private set; }

    /// <summary>
    /// If the scanner has reached the end of the source code
    /// </summary>
    private unsafe bool IsEOF
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => *this.currentChar == char.MinValue;
    }

    /// <summary>
    /// If the scanner has reached the end of the source code
    /// </summary>
    private unsafe int CurrentTokenLength
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (int)(this.currentChar - this.tokenStart);
    }
    #endregion

    #region Constructors
    /// <summary>
    /// Creates a new scanner without any source code specified
    /// </summary>
    public LoxScanner() : this(string.Empty) { }

    /// <summary>
    /// Creates a new scanner with the specified source code
    /// </summary>
    /// <param name="source">Source code</param>
    /// <exception cref="ArgumentNullException">If <paramref name="source"/> is <see langword="null"/></exception>
    public LoxScanner(string source) => this.sourceCode = source ?? throw new ArgumentNullException(nameof(source), "Source code cannot be null");

    /// <summary>
    /// Scanner finalizer, ensures that the source code is unpinned
    /// </summary>
    ~LoxScanner() => FreeSource();
    #endregion

    #region Methods
    /// <summary>
    /// Pins the current source code in the GC to allows scanning tokens<br/>
    /// Must be unpinned with <see cref="FreeSource"/>
    /// </summary>
    /// <param name="source">Source code to pin, if not specifies, defaults to the value in <see cref="SourceCode"/></param>
    /// <exception cref="ObjectDisposedException">If this scanner has been disposed</exception>
    /// <exception cref="InvalidOperationException">If the scanner is already scanning a source string</exception>
    public unsafe void PinSource(string? source = null)
    {
        ObjectDisposedException.ThrowIf(this.IsDisposed, this);
        ThrowIfScanningState(this.IsScanning);

        if (!string.IsNullOrEmpty(source))
        {
            this.sourceCode = source;
        }

        this.sourceHandle = GCHandle.Alloc(this.sourceCode, GCHandleType.Pinned);
        this.tokenStart   = (char*)this.sourceHandle.AddrOfPinnedObject();
        this.currentChar  = this.tokenStart;
        this.currentLine  = 1;
        this.returnedEof  = false;
    }

    /// <summary>
    /// Unpins the current source code from the GC
    /// </summary>
    /// <exception cref="ObjectDisposedException">If this scanner has been disposed</exception>
    public unsafe void FreeSource()
    {
        ObjectDisposedException.ThrowIf(this.IsDisposed, this);
        if (!this.IsScanning) return;

        this.sourceHandle.Free();
        this.tokenStart  = null;
        this.currentChar = null;
    }

    /// <summary>
    /// Opens a new scope which pins the source code
    /// </summary>
    /// <param name="source">Source code to pin, otherwise defaults to <see cref="SourceCode"/></param>
    /// <returns>A disposable scope object which ensures the pin is successfully released</returns>
    /// <exception cref="ObjectDisposedException">If this scanner has been disposed</exception>
    /// <exception cref="InvalidOperationException">If the scanner is already scanning a source string</exception>
    public PinScope OpenPinScope(string? source = null) => new(this, source ?? this.sourceCode);

    /// <summary>
    /// Gets an enumerator over this scanner<br/>
    /// This will pin the source code in the GC, which will be freed once the enumerator is disposed
    /// </summary>
    /// <returns>The token enumerator of this scanner</returns>
    /// <exception cref="ObjectDisposedException">If this scanner has been disposed</exception>
    /// <exception cref="InvalidOperationException">If the scanner is already scanning a source string</exception>
    public TokenEnumerator GetEnumerator()
    {
        ObjectDisposedException.ThrowIf(this.IsDisposed, this);
        ThrowIfScanningState(this.IsScanning);
        PinSource();
        return new TokenEnumerator(this);
    }

    /// <summary>
    /// Disposes this scanner and frees any pins it may currently hold
    /// </summary>
    /// <exception cref="ObjectDisposedException">If this scanner has been disposed</exception>
    public void Dispose()
    {
        if (this.IsDisposed) return;

        FreeSource();
        GC.SuppressFinalize(this);
        this.IsDisposed = true;
    }

    /// <summary>
    /// Checks if the next character in the source matches the given char, and consumes it if it is
    /// </summary>
    /// <param name="toMatch">Character to match</param>
    /// <returns><see langword="true"/> if the next source char matches <paramref name="toMatch"/>, otherwise <see langword="false"/></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe bool MatchNext(char toMatch)
    {
        if (this.IsEOF || *this.currentChar != toMatch) return false;

        this.currentChar++;
        return true;
    }

    /// <summary>
    /// Consumes characters in the source until the specified terminator is found, or the source is fully consumed
    /// </summary>
    /// <param name="terminator">Terminator character to stop on</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ConsumeUntil(char terminator)
    {
        for (char current = NextChar(); !this.IsEOF && current != terminator; current = NextChar());
    }

    /// <summary>
    /// Returns the next character in the source and increments the current index
    /// </summary>
    /// <returns>The next source character</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining), MustUseReturnValue("Use Next if not consuming the character")]
    private unsafe char NextChar() => *this.currentChar++;

    /// <summary>
    /// Decrements the current index
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void Rewind() => this.currentChar--;

    /// <summary>
    /// Peeks at the current character in the source without incrementing the current index
    /// </summary>
    /// <returns>The next source character, or if at the end of the source, <c>\0</c></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe char CurrentChar() => *this.currentChar;

    /// <summary>
    /// Peeks at the next character ahead in the source without incrementing the current index
    /// </summary>
    /// <param name="peekDistance">Look-ahead distance</param>
    /// <returns>The source code character at the given distance ahead of the current index, or <c>\0</c> if the peek index is outside of the source code bounds</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe char PeekChar(int peekDistance = 1) => *(this.currentChar + peekDistance);

    /// <summary>
    /// Increments the index by a certain amount and returns the character at that given position
    /// </summary>
    /// <param name="skipDistance">Skip-ahead distance</param>
    /// <returns>The source code character at the given distance ahead of the current index, or <c>\0</c> if the peek index is outside of the source code bounds</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe char SkipChar(int skipDistance = 1) => *(this.currentChar += skipDistance);

    /// <summary>
    /// Resets the token start to the current character
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void ResetStart() => this.tokenStart = this.currentChar;

    /// <summary>
    /// Resets the token start to the previous character
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void ResetStartToPrevious() => this.tokenStart = this.currentChar - 1;

    /// <summary>
    /// Makes a new string for the current token's lexeme
    /// </summary>
    /// <returns>Current token lexeme</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe string GetCurrentLexeme() => new(this.tokenStart, 0, this.CurrentTokenLength);
    #endregion

    #region Static methods
    /// <summary>
    /// Checks if the given character is a valid word character
    /// </summary>
    /// <param name="character">Character to test</param>
    /// <returns><see langword="true"/> if <paramref name="character"/> is an ascii letter or an underscore, otherwise <see langword="false"/></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsWordChar(char character) => char.IsAsciiLetter(character) || character is '_';

    /// <summary>
    /// Checks if the given character is a valid identifier character
    /// </summary>
    /// <param name="character">Character to test</param>
    /// <returns><see langword="true"/> if <paramref name="character"/> is an ascii letter, ascii digit, or an underscore, otherwise <see langword="false"/></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsIdentifierChar(char character) => IsWordChar(character) || char.IsAsciiDigit(character);

    /// <summary>
    /// Throws an <see cref="InvalidOperationException"/> if <paramref name="scanningState"/> is <see langword="true"/>
    /// </summary>
    /// <param name="scanningState">Scanning state to throw on</param>
    /// <param name="message">Exception message, defaults to "Scanner already mid-scan"</param>
    /// <exception cref="InvalidOperationException">When <paramref name="scanningState"/> is <see langword="true"/></exception>
    private static void ThrowIfScanningState([DoesNotReturnIf(true)] bool scanningState, string? message = null)
    {
        if (scanningState)
        {
            throw new InvalidOperationException(message ?? "Scanner already mid-scan");
        }
    }
    #endregion

    #region Explicit implementations
    /// <summary>
    /// Sets up this scanner to tokenize a given source code string
    /// </summary>
    /// <param name="source">Source code string</param>
    /// <returns>The current scanner</returns>
    /// <exception cref="ObjectDisposedException">If this scanner has been disposed</exception>
    /// <exception cref="InvalidOperationException">If the scanner is already scanning a source string</exception>
    /// <exception cref="ArgumentNullException">If <paramref name="source"/> is <see langword="null"/></exception>
    IEnumerable<Token> ILoxScanner<Token>.Tokenize(string source)
    {
        ObjectDisposedException.ThrowIf(this.IsDisposed, this);
        ThrowIfScanningState(this.IsScanning);

        this.sourceCode = source ?? throw new ArgumentNullException(nameof(source), "Source code cannot be null");
        return this;
    }

    /// <inheritdoc />
    IEnumerator<Token> IEnumerable<Token>.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    #endregion
}

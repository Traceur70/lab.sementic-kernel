

namespace Lab.SemanticKernel.TestingConsole;

public static class Helpers
{
    public async static Task<T> FirstAsync<T>(this IAsyncEnumerable<T> myAsyncEnumerable)
    {
        await using var enumerator = myAsyncEnumerable.GetAsyncEnumerator();
        return !await enumerator.MoveNextAsync() ? throw new Exception() : enumerator.Current ?? throw new Exception();
    }
}

using System.Linq.Expressions;
using System.Linq.Dynamic.Core;
using API_Backend.Models;

namespace API_Backend.Services.FileProcessing
{
    public class ExpressionParser
    {

        /// <summary>
        /// Takes an input string containing an expression and parses it into an expression with type T and return of type boolean.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static Expression<Func<T, bool>> ParseExpression<T>(string expression, string param)
        {
            var parameter = Expression.Parameter(typeof(T), param);
            return (Expression<Func<T, bool>>) DynamicExpressionParser.ParseLambda(new[] { parameter }, null, expression);
        }

    }
}

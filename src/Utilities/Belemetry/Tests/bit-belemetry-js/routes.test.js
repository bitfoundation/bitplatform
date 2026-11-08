import { describe, expect, it } from 'vitest';
import { compileRoutes, matchRoute, pageTemplate } from '../../Bit.Belemetry/Scripts/routes.ts';

const match = (routes, path) => pageTemplate(path, routes);

describe('page route templates', () => {
    it('prefers literals to constrained parameters to parameters to catch-alls, as the Blazor router does', () => {
        const routes = ['/{*path}', '/orders/{id}', '/orders/{id:int}', '/orders/new', '/orders/{**rest:nonfile}', '/orders'];
        expect(compileRoutes(routes).map(r => r.template)).toEqual(['/orders', '/orders/new', '/orders/{id:int}', '/orders/{id}', '/orders/{**rest:nonfile}', '/{*path}']);
        expect(match(routes, '/orders')).toBe('/orders');
        expect(match(routes, '/orders/new')).toBe('/orders/new');
        expect(match(routes, '/orders/42')).toBe('/orders/{id:int}');
        expect(match(routes, '/orders/abc')).toBe('/orders/{id}');
        expect(match(routes, '/orders/a/b')).toBe('/orders/{**rest:nonfile}');
        expect(match(routes, '/orders/a/b.txt')).toBe('/{*path}');
        expect(match(routes, '/')).toBe('/{*path}');
    });

    it('matches case-insensitively, decodes segments and ignores empty ones', () => {
        const routes = ['/Users/{name}/Profile'];
        expect(match(routes, '/users/jane%20doe/PROFILE/')).toBe('/Users/{name}/Profile');
        expect(match(routes, '//users//jdoe//profile')).toBe('/Users/{name}/Profile');
        expect(match(routes, '/users/jdoe')).toBe('/users/jdoe');
    });

    it('lets optional parameters, defaults and catch-alls be left out', () => {
        const routes = ['/search/{term?}', '/page/{n:int=1}', '/docs/{*slug}'];
        expect(match(routes, '/search')).toBe('/search/{term?}');
        expect(match(routes, '/search/shoes')).toBe('/search/{term?}');
        expect(match(routes, '/search/shoes/red')).toBe('/search/shoes/red');
        expect(match(routes, '/page')).toBe('/page/{n:int=1}');
        expect(match(routes, '/page/x')).toBe('/page/x');
        expect(match(routes, '/docs')).toBe('/docs/{*slug}');
        expect(match(routes, '/docs/a/b/c')).toBe('/docs/{*slug}');
    });

    it('checks the constraints Blazor and ASP.NET Core define', () => {
        const cases = [
            ['{v:int}', ['42', '-7', '2147483647'], ['2147483648', '4.2', 'x']],
            ['{v:long}', ['9223372036854775807', '-9223372036854775808'], ['9223372036854775808', 'x']],
            ['{v:bool}', ['true', 'FALSE'], ['yes', '1']],
            ['{v:guid}', ['3f2a8c1e-0b6d-4f3a-9c1e-2b6d4f3a9c1e', '3f2a8c1e0b6d4f3a9c1e2b6d4f3a9c1e', '{3f2a8c1e-0b6d-4f3a-9c1e-2b6d4f3a9c1e}'], ['3f2a8c1e', 'x']],
            ['{v:decimal}', ['4.2', '-1', '1,000.5'], ['1e3', 'x']],
            ['{v:double}', ['4.2', '1e3', 'NaN'], ['x']],
            // As .NET's invariant DateTime.TryParse: a bare number is no date, though Date.parse takes it.
            ['{v:datetime}', ['2026-11-08', '2026-11-08T10:30', '2024-01-02T10:30:00Z', '1/2/2024', '1/2/24', '2024/1', '12-2024', '1.5', '10:30', '10:30 PM', 'Jan 2 2024'],
                ['not a date', '7', '42', '12345', '2024', 'abc 5', '2024-13-01', '2024-02-30', '13/01/2024', '25:00', '24-01-02', '2024-001', '2024-01-02T10']],
            ['{v:alpha}', ['abc'], ['ab1']],
            ['{v:nonfile}', ['readme'], ['readme.md']],
            ['{v:file}', ['readme.md', 'a.b.'], ['readme', 'readme.']],
            ['{v:length(3)}', ['abc'], ['ab']],
            ['{v:length(2,3)}', ['ab', 'abc'], ['a', 'abcd']],
            ['{v:minlength(2):maxlength(3)}', ['ab'], ['a', 'abcd']],
            ['{v:min(10)}', ['10'], ['9', 'x']],
            ['{v:max(10)}', ['10'], ['11']],
            ['{v:range(1,3)}', ['2'], ['4']],
            ['{v:regex(^\\d{{3}}$)}', ['123'], ['12', '1234']],
            ['{v:custom}', ['anything'], []],
        ];
        for (const [parameter, good, bad] of cases) {
            const [route] = compileRoutes(['/x/' + parameter]);
            for (const value of good) expect(matchRoute(route, ['x', value]), parameter + ' ' + value).toBe(true);
            for (const value of bad) expect(matchRoute(route, ['x', value]), parameter + ' ' + value).toBe(false);
        }
    });

    it('does not let a datetime route claim a number', () => {
        const routes = ['/x/{d:datetime}', '/x/{n:int}'];
        expect(match(routes, '/x/42')).toBe('/x/{n:int}');
        expect(match(routes, '/x/2026-11-08')).toBe('/x/{d:datetime}');
        expect(match(['/archive/{date:datetime}'], '/archive/2')).toBe('/archive/{n}');
    });

    it('matches segments of several parts from the right, and skips malformed templates', () => {
        const routes = ['/files/{name}.{ext}', '/broken/{id', '/files/{name}'];
        expect(compileRoutes(routes).map(r => r.template)).toEqual(['/files/{name}.{ext}', '/files/{name}']);
        expect(match(routes, '/files/report.pdf')).toBe('/files/{name}.{ext}');
        expect(match(routes, '/files/report')).toBe('/files/{name}');
        expect(match(routes, '/broken/1')).toBe('/broken/{n}');

        // archive.tar + gz, as ASP.NET Core splits it - not archive + tar.gz.
        expect(match(['/files/{name}.{ext:alpha}'], '/files/archive.tar.gz')).toBe('/files/{name}.{ext:alpha}');
        expect(match(['/f/{a}-{b:int}'], '/f/x-y-2')).toBe('/f/{a}-{b:int}');
        expect(match(['/f/{a}-{b:int}'], '/f/x-2-y')).toBe('/f/x-2-y');
        expect(match(['/p/a{x}'], '/p/ab')).toBe('/p/a{x}');
        expect(match(['/p/a{x}'], '/p/ba1')).toBe('/p/ba1');
    });

    it('lets an optional last part be left out with its dot', () => {
        const routes = ['/files/{name}.{ext?}'];
        expect(match(routes, '/files/report.pdf')).toBe('/files/{name}.{ext?}');
        expect(match(routes, '/files/report')).toBe('/files/{name}.{ext?}');
        expect(match(routes, '/files/report.')).toBe('/files/report.');
    });

    it('lists a template once, however it is written', () => {
        expect(compileRoutes(['/a/', 'a', '/A', '~/a', '', '/']).map(r => r.template)).toEqual(['/', '/a']);
    });
});

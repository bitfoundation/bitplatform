// A minimal extension module, the shape an application ships to add its own SQL functions.
export function register({ sqlite3, db }) {
  sqlite3.create_function(db, 'shout', 1, 1 /* SQLITE_UTF8 */, 0, (ctx, values) => {
    sqlite3.result(ctx, `${sqlite3.value_text(values[0]).toUpperCase()}!`);
  });
}

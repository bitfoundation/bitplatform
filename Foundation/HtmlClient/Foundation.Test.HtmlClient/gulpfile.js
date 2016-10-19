var gulp = require("gulp");
var sourcemaps = require("gulp-sourcemaps");
var babel = require("gulp-babel");
var bower = require('gulp-bower');

gulp.task("ts-babel", function () {
    return gulp.src("foundation.test.js")
        .pipe(sourcemaps.init({ loadMaps: true }))
        .pipe(babel({
            presets: ["es2015-loose"]
        }))
        .pipe(sourcemaps.write('.'))
        .pipe(gulp.dest('./'));
});

gulp.task('bower', function () {
    return bower({ cmd: 'install' });
});
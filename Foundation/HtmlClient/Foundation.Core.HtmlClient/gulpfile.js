var gulp = require("gulp");
var sourcemaps = require("gulp-sourcemaps");
var babel = require("gulp-babel");

gulp.task("ts-babel", function () {
    return gulp.src("foundation.core.js")
        .pipe(sourcemaps.init({ loadMaps: true }))
        .pipe(babel({
            presets: ["es2015-loose"]
        }))
        .pipe(sourcemaps.write('.'))
        .pipe(gulp.dest('./'));
});
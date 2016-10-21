/// <binding AfterBuild='less' />
var gulp = require("gulp");
var sourcemaps = require("gulp-sourcemaps");
var babel = require("gulp-babel");
var less = require("gulp-less");

gulp.task("ts-babel", function () {
    return gulp.src("identityServer.htmlClient.js")
        .pipe(sourcemaps.init({ loadMaps: true }))
        .pipe(babel({
            presets: ["es2015-loose"]
        }))
        .pipe(sourcemaps.write('.'))
        .pipe(gulp.dest('./'));
});

gulp.task("less", function () {
    gulp.src(["./view/contents/styles/*.less"])
        .pipe(less())
        .pipe(gulp.dest("./view/contents/styles/"));
});
/// <binding AfterBuild='less' />
var gulp = require("gulp");
var less = require("gulp-less");

gulp.task("less", function () {
    gulp.src(["./view/contents/styles/*.less"])
        .pipe(less())
        .pipe(gulp.dest("./view/contents/styles/"));
});
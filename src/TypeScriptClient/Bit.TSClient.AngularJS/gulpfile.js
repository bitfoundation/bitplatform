/// <binding AfterBuild="less" />
var gulp = require("gulp");
var less = require("gulp-less");
var path = require("path");
 
gulp.task("less", function () {
  return gulp.src("./contents/styles/*.less")
    .pipe(less({
      paths: [ path.join(__dirname, "less", "includes") ]
    }))
    .pipe(gulp.dest("./contents/styles/"));
});

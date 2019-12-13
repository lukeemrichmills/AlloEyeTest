hold off
figure()
tableDiameter = 0.8; % dimension of box collider of table (i.e. the fixation zone)
scale = 50;
tableX = 0.0830; %X-centre of table for offsetting
tableZ = 0.7171; %Z-centre of table for offsetting
offsetX = scale*(tableX-(tableDiameter/2));
offsetZ = scale*(tableZ-(tableDiameter/2));
N = 0.8*scale; %set 'pixels' of heatmap
x = linspace(0,1,N) ;
y = linspace(0,1,N) ;
T = zeros(N) ;

for i=1:height(fp_full)
    xc = round((fp_full.X(i)*scale)-offsetX);
    yc = round((fp_full.Z(i)*scale)-offsetZ);
    T(xc,yc) = 1;
end

pcolor(x,y,T) ;
colorbar
shading interp